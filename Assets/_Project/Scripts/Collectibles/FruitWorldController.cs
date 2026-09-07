using System;
using System.Collections.Generic;
using ProjectSparks.Player;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Collectibles
{
    [DisallowMultipleComponent]
    public sealed class FruitWorldController : MonoBehaviour, IFruitWorldService
    {
        [Header("Prefab фруктов")]
        [Tooltip("Физические prefab пяти типов фруктов. Каждый prefab должен иметь PhysicalFruitItem.")]
        [SerializeField] private PhysicalFruitItem[] _fruitPrefabs;

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

        private readonly Dictionary<FruitType, PhysicalFruitItem> _prefabs = new();
        private readonly Dictionary<FruitType, Stack<PhysicalFruitItem>> _pool = new();
        private readonly LinkedList<PhysicalFruitItem> _droppedFruits = new();
        private readonly Dictionary<PhysicalFruitItem, LinkedListNode<PhysicalFruitItem>>
            _droppedNodes = new();

        private IPlayerHandsService _handsService;
        private IResourceInventoryService _inventoryService;
        private Transform _poolRoot;
        private bool _isReady;
        private bool _isShuttingDown;

        [Inject]
        private void Construct(
            IPlayerHandsService handsService,
            IResourceInventoryService inventoryService)
        {
            _handsService = handsService;
            _inventoryService = inventoryService;
        }

        private void Awake()
        {
            _worldRoot ??= transform;
            CreatePoolRoot();
            _isReady = BuildPrefabMap();
            if (!_isReady)
            {
                Debug.LogError(
                    "В FruitWorldController должны быть назначены физические prefab всех пяти типов фруктов.",
                    this);
                enabled = false;
            }
        }

        public bool TryTakeFromCrate(FruitType fruitType)
        {
            if (!_isReady
                || _handsService.HasItem
                || !_inventoryService.TryRemoveFruit(fruitType, 1))
            {
                return false;
            }

            PhysicalFruitItem fruit = Rent(fruitType);
            fruit.PrepareForHand(_poolRoot);
            if (_handsService.TryHold(fruit))
            {
                return true;
            }

            ReturnToPool(fruit);
            _inventoryService.AddFruit(fruitType, 1);
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

            _inventoryService.AddFruit(fruit.FruitType, 1);
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

        private bool BuildPrefabMap()
        {
            _prefabs.Clear();
            _pool.Clear();

            if (_fruitPrefabs == null)
            {
                return false;
            }

            foreach (PhysicalFruitItem prefab in _fruitPrefabs)
            {
                if (prefab == null || _prefabs.ContainsKey(prefab.FruitType))
                {
                    continue;
                }

                _prefabs.Add(prefab.FruitType, prefab);
                _pool.Add(prefab.FruitType, new Stack<PhysicalFruitItem>());
            }

            return _prefabs.Count == Enum.GetValues(typeof(FruitType)).Length;
        }

        private PhysicalFruitItem Rent(FruitType fruitType)
        {
            Stack<PhysicalFruitItem> available = _pool[fruitType];
            PhysicalFruitItem fruit = available.Count > 0
                ? available.Pop()
                : Instantiate(_prefabs[fruitType], _poolRoot);
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
            _pool[fruit.FruitType].Push(fruit);
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
                _inventoryService.AddFruit(oldest.FruitType, 1);
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
            if (_isShuttingDown || _inventoryService == null)
            {
                return;
            }

            _isShuttingDown = true;
            if (_handsService?.HeldItem is PhysicalFruitItem heldFruit)
            {
                _inventoryService.AddFruit(heldFruit.FruitType, 1);
            }

            foreach (PhysicalFruitItem droppedFruit in _droppedFruits)
            {
                if (droppedFruit != null)
                {
                    _inventoryService.AddFruit(droppedFruit.FruitType, 1);
                }
            }
        }
    }
}
