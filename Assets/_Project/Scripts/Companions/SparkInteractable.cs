using ProjectSparks.Collectibles;
using ProjectSparks.Player;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    public sealed class SparkInteractable : MonoBehaviour, IInteractable, IHoldableItem, ISparkProfile
    {
        [Header("Искра")]
        [Tooltip("Имя Искры, которое отображается в интерфейсе осмотра.")]
        [SerializeField] private string _displayName = "Искра";
        [Tooltip("Игровой тип способности этой Искры.")]
        [SerializeField] private CompanionType _companionType = CompanionType.Collector;
        [Tooltip("Краткое описание способности Искры.")]
        [SerializeField, TextArea] private string _abilityDescription = "Увеличивает радиус сбора ресурсов на 50%.";
        [Tooltip("Коллайдеры Искры, отключаемые на время нахождения в руках.")]
        [SerializeField] private Collider[] _colliders;

        private IPlayerHandsService _handsService;
        private ISparkProgressService _sparkProgressService;
        private IShelterInteractionRequestService _requestService;
        private bool _isHeld;

        public string DisplayName => _sparkProgressService != null
            ? _sparkProgressService.GetProgress(_companionType).DisplayName
            : _displayName;
        public string AbilityDescription => _abilityDescription;
        public CompanionType CompanionType => _companionType;
        public HeldItemType ItemType => HeldItemType.Spark;
        public Transform ItemTransform => transform;
        public string InteractionPrompt =>
            _handsService.HeldItem is IFruitItem
                ? "Покормить Искру"
                : "Взять Искру";
        public bool CanInteract => !_isHeld
            && (!_handsService.HasItem
                || _handsService.HeldItem is IFruitItem);

        [Inject]
        private void Construct(
            IPlayerHandsService handsService,
            ISparkProgressService sparkProgressService,
            IShelterInteractionRequestService requestService)
        {
            _handsService = handsService;
            _sparkProgressService = sparkProgressService;
            _requestService = requestService;
        }

        private void Awake()
        {
            if (_colliders == null || _colliders.Length == 0)
            {
                _colliders = GetComponentsInChildren<Collider>(true);
            }
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            if (_handsService.HeldItem is IFruitItem fruit)
            {
                _requestService.RequestSparkFeeding(this, fruit);
            }
            else
            {
                _handsService.TryHold(this);
            }
        }

        public void SetHeld(bool isHeld)
        {
            _isHeld = isHeld;
            SetCollidersEnabled(!isHeld);
        }

        private void SetCollidersEnabled(bool isEnabled)
        {
            if (_colliders == null)
            {
                return;
            }

            foreach (Collider itemCollider in _colliders)
            {
                if (itemCollider != null)
                {
                    itemCollider.enabled = isEnabled;
                }
            }
        }
    }
}
