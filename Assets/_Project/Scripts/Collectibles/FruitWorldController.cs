using System;
using System.Collections.Generic;
using ProjectSparks.Companions;
using ProjectSparks.Player;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Collectibles
{
    [DisallowMultipleComponent]
    public sealed class FruitWorldController : MonoBehaviour, IFruitWorldService
    {
        [Header("Размещение")]
        [Tooltip("Точка, перед которой появляется выброшенный фрукт.")]
        [SerializeField] private Transform _dropOrigin;
        [Tooltip("Корень физических фруктов, лежащих в комнате.")]
        [SerializeField] private Transform _worldRoot;
        [Tooltip("Расстояние появления фрукта перед точкой броска.")]
        [SerializeField, Min(0.1f)] private float _dropDistance = 0.8f;
        [Tooltip("Начальная скорость выброшенного фрукта.")]
        [SerializeField, Min(0f)] private float _dropSpeed = 2f;
        [Tooltip("Дополнительная вертикальная скорость броска.")]
        [SerializeField, Min(0f)] private float _dropUpwardSpeed = 0.5f;
        [Tooltip("Максимальное количество фруктов, одновременно лежащих в комнате.")]
        [SerializeField, Min(1)] private int _maxDroppedFruits = 6;

        private readonly Dictionary<string, PhysicalFruitItem> _prefabs = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Stack<PhysicalFruitItem>> _pool = new(StringComparer.Ordinal);
        private readonly LinkedList<PhysicalFruitItem> _droppedFruits = new();
        private readonly Dictionary<PhysicalFruitItem, LinkedListNode<PhysicalFruitItem>>
            _droppedNodes = new();
        private readonly Dictionary<ISparkStateController, PhysicalFruitItem> _presentedFruits = new();
        private readonly Dictionary<ISparkStateController, SparkFruitAttachment> _fruitAttachments = new();
        private readonly Dictionary<ISparkStateController, Action<SparkBehaviourState>> _presentationHandlers = new();
        private readonly Dictionary<ISparkStateController, Action> _consumptionHandlers = new();

        private IPlayerHandsService _handsService;
        private IResourceInventoryService _inventoryService;
        private ISparkFeedingService _feedingService;
        private FruitCatalog _catalog;
        private Transform _poolRoot;
        private bool _isReady;
        private bool _isShuttingDown;

        [Inject]
        private void Construct(
            IPlayerHandsService handsService,
            IResourceInventoryService inventoryService,
            ISparkFeedingService feedingService,
            FruitCatalog catalog)
        {
            _handsService = handsService;
            _inventoryService = inventoryService;
            _feedingService = feedingService;
            _catalog = catalog;
        }

        private void Awake()
        {
            _worldRoot ??= transform;
            CreatePoolRoot();
        }

        private void Start()
        {
            _isReady = BuildPrefabMap();
            if (!_isReady)
            {
                Debug.LogError( 
                    "FruitWorldController: каталог фруктов пуст или содержит недопустимые записи.",
                    this);
                enabled = false;
            }
        }

        public bool TryTakeFromCrate(string fruitId)
        {
            if (!_isReady
                || string.IsNullOrWhiteSpace(fruitId)
                || !_prefabs.ContainsKey(fruitId)
                || _handsService.HasItem
                || !_inventoryService.TryRemoveFruit(fruitId, 1))
            {
                return false;
            }

            PhysicalFruitItem fruit = Rent(fruitId);
            fruit.PrepareForHand(_poolRoot);
            if (_handsService.TryHold(fruit))
            {
                return true;
            }

            ReturnToPool(fruit);
            _inventoryService.AddFruit(fruitId, 1);
            return false;
        }

        public bool TryPickUp(PhysicalFruitItem fruit)
        {
            if (!_isReady
                || fruit == null
                || _handsService.HasItem
                || !_droppedNodes.ContainsKey(fruit))
            {
                return false;
            }

            if (!_handsService.TryHold(fruit))
            {
                return false;
            }

            UnregisterDropped(fruit);
            return true;
        }

        public bool TryDropHeldFruit()
        {
            if (!_isReady
                || _dropOrigin == null
                || _handsService.HeldItem is not PhysicalFruitItem fruit)
            {
                return false;
            }

            Vector3 position = _dropOrigin.position
                + _dropOrigin.forward * _dropDistance;
            Vector3 velocity = _dropOrigin.forward * _dropSpeed
                + Vector3.up * _dropUpwardSpeed;
            Quaternion rotation = UnityEngine.Random.rotation;

            if (!_handsService.TryRelease())
            {
                return false;
            }

            fruit.PlaceInWorld(_worldRoot, position, rotation, velocity);
            RegisterDropped(fruit);
            EnforceDroppedLimit();
            return true;
        }

        public bool TryReturnHeldFruitToCrate()
        {
            if (!_isReady
                || _handsService.HeldItem is not PhysicalFruitItem fruit
                || !_handsService.TryRelease())
            {
                return false;
            }

            _inventoryService.AddFruit(fruit.FruitId, 1);
            ReturnToPool(fruit);
            return true;
        }

        public bool TryConsumeHeldFruit()
        {
            if (!_isReady
                || _handsService.HeldItem is not PhysicalFruitItem fruit
                || !_handsService.TryRelease())
            {
                return false;
            }

            ReturnToPool(fruit);
            return true;
        }

        public bool TryPresentHeldFruit(Transform anchor, ISparkStateController spark)
        {
            if (!_isReady || anchor == null || spark == null
                || spark.State != SparkBehaviourState.Eating
                || _presentedFruits.ContainsKey(spark)
                || _handsService.HeldItem is not PhysicalFruitItem fruit)
            {
                return false;
            }

            SparkFruitAttachment attachment = anchor.GetComponentInParent<SparkFruitAttachment>();
            if (attachment == null || !attachment.isActiveAndEnabled
                || fruit.LeftHandTarget == null || fruit.RightHandTarget == null
                || !_handsService.TryRelease())
            {
                return false;
            }

            fruit.transform.SetParent(anchor, false);
            fruit.transform.localPosition = Vector3.zero;
            fruit.transform.localRotation = Quaternion.identity;
            _fruitAttachments.Add(spark, attachment);
            attachment.SetFruit(fruit);
            _presentedFruits.Add(spark, fruit);

            Action<SparkBehaviourState> handler = state =>
            {
                if (state != SparkBehaviourState.Eating)
                {
                    DropPresentedFruit(spark);
                }
            };
            Action consumedHandler = () => ConsumePresentedFruit(spark);
            _presentationHandlers.Add(spark, handler);
            _consumptionHandlers.Add(spark, consumedHandler);
            spark.StateChanged += handler;
            spark.EatingAnimationCompleted += consumedHandler;
            return true;
        }

        private void ConsumePresentedFruit(ISparkStateController spark)
        {
            if (!TakePresentedFruit(spark, out PhysicalFruitItem fruit))
            {
                return;
            }

            if (_feedingService.TryFeed(spark.Id, fruit.FruitId))
            {
                ReturnToPool(fruit);
            }
            else
            {
                DropFruitBesideSpark(spark, fruit);
            }
        }

        private void DropPresentedFruit(ISparkStateController spark)
        {
            if (TakePresentedFruit(spark, out PhysicalFruitItem fruit))
            {
                DropFruitBesideSpark(spark, fruit);
            }
        }

        private bool TakePresentedFruit(ISparkStateController spark, out PhysicalFruitItem fruit)
        {
            if (!_presentedFruits.TryGetValue(spark, out fruit))
            {
                return false;
            }

            spark.StateChanged -= _presentationHandlers[spark];
            spark.EatingAnimationCompleted -= _consumptionHandlers[spark];
            _presentationHandlers.Remove(spark);
            _consumptionHandlers.Remove(spark);
            _presentedFruits.Remove(spark);
            if (_fruitAttachments.Remove(spark, out SparkFruitAttachment attachment)
                && attachment != null)
            {
                attachment.SetFruit(null);
            }
            return true;
        }

        private void DropFruitBesideSpark(ISparkStateController spark, PhysicalFruitItem fruit)
        {
            Vector3 sparkPosition = spark is Component component && component != null
                ? component.transform.position
                : fruit.transform.position;
            Vector3 outward = Vector3.ProjectOnPlane(
                fruit.transform.position - sparkPosition, Vector3.up);
            if (outward.sqrMagnitude < 0.0001f)
            {
                outward = spark is Component owner && owner != null
                    ? owner.transform.forward
                    : Vector3.forward;
            }
            outward = Vector3.ProjectOnPlane(outward, Vector3.up).normalized;

            Vector3 position = sparkPosition + outward * _dropDistance;
            position.y = Mathf.Max(fruit.transform.position.y, sparkPosition.y + 0.3f);
            Vector3 velocity = outward * _dropSpeed + Vector3.up * _dropUpwardSpeed;
            fruit.PlaceInWorld(_worldRoot, position, fruit.transform.rotation, velocity);
            RegisterDropped(fruit);
            EnforceDroppedLimit();
        }

        private bool BuildPrefabMap()
        {
            _prefabs.Clear();
            _pool.Clear();

            if (_catalog == null || _catalog.Count == 0 || !_catalog.TryValidate(out _))
            {
                return false;
            }

            for (int i = 0; i < _catalog.Count; i++)
            {
                PhysicalFruitItem prefab = _catalog.GetEntry(i).Prefab;
                _prefabs.Add(prefab.FruitId, prefab);
                _pool.Add(prefab.FruitId, new Stack<PhysicalFruitItem>());
            }

            return _prefabs.Count == _catalog.Count;
        }

        private PhysicalFruitItem Rent(string fruitId)
        {
            Stack<PhysicalFruitItem> available = _pool[fruitId];
            PhysicalFruitItem fruit = available.Count > 0
                ? available.Pop()
                : Instantiate(_prefabs[fruitId], _poolRoot);
            fruit.Initialize(this);
            return fruit;
        }

        private void ReturnToPool(PhysicalFruitItem fruit)
        {
            if (fruit == null)
            {
                return;
            }

            UnregisterDropped(fruit);
            fruit.ReturnToPool(_poolRoot);
            _pool[fruit.FruitId].Push(fruit);
        }

        private void RegisterDropped(PhysicalFruitItem fruit)
        {
            UnregisterDropped(fruit);
            LinkedListNode<PhysicalFruitItem> node = _droppedFruits.AddLast(fruit);
            _droppedNodes.Add(fruit, node);
        }

        private void UnregisterDropped(PhysicalFruitItem fruit)
        {
            if (!_droppedNodes.TryGetValue(fruit, out LinkedListNode<PhysicalFruitItem> node))
            {
                return;
            }

            _droppedNodes.Remove(fruit);
            _droppedFruits.Remove(node);
        }

        private void EnforceDroppedLimit()
        {
            while (_droppedFruits.Count > _maxDroppedFruits)
            {
                PhysicalFruitItem oldest = _droppedFruits.First.Value;
                _inventoryService.AddFruit(oldest.FruitId, 1);
                ReturnToPool(oldest);
            }
        }

        private void CreatePoolRoot()
        {
            GameObject root = new GameObject("FruitPool");
            root.transform.SetParent(transform, false);
            _poolRoot = root.transform;
        }

        private void OnDestroy()
        {
            foreach (var handler in _presentationHandlers)
            {
                handler.Key.StateChanged -= handler.Value;
            }
            foreach (var handler in _consumptionHandlers)
            {
                handler.Key.EatingAnimationCompleted -= handler.Value;
            }

            _presentationHandlers.Clear();
            _consumptionHandlers.Clear();
            foreach (SparkFruitAttachment attachment in _fruitAttachments.Values)
            {
                if (attachment != null) attachment.SetFruit(null);
            }
            _fruitAttachments.Clear();

            if (_isShuttingDown || _inventoryService == null)
            {
                return;
            }

            _isShuttingDown = true;
            foreach (PhysicalFruitItem presentedFruit in _presentedFruits.Values)
            {
                if (presentedFruit != null)
                {
                    _inventoryService.AddFruit(presentedFruit.FruitId, 1);
                }
            }
            _presentedFruits.Clear();
            if (_handsService?.HeldItem is PhysicalFruitItem heldFruit)
            {
                _inventoryService.AddFruit(heldFruit.FruitId, 1);
            }

            foreach (PhysicalFruitItem droppedFruit in _droppedFruits)
            {
                if (droppedFruit != null)
                {
                    _inventoryService.AddFruit(droppedFruit.FruitId, 1);
                }
            }
        }
    }
}
