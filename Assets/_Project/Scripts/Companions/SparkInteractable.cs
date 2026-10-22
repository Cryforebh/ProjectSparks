using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Collectibles;
using ProjectSparks.Player;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Companions
{
    public enum SparkBehaviourState
    {
        Standing = 0,
        Held = 1,
        Eating = 2,
        Editing = 3,
        Moving = 4,
        Resting = 5
    }

    public interface ISparkStateController
    {
        event Action<SparkBehaviourState> StateChanged;
        event Action EatingAnimationCompleted;

        string Id { get; }
        SparkBehaviourState State { get; }

        bool TryStartEating();
        void CompleteEating();
    }

    [DisallowMultipleComponent]
    public sealed class SparkInteractable : MonoBehaviour,
        IInteractable,
        IHoldableItem,
        ISparkProfile,
        ISparkStateController
    {
        [Header("Искра")]
        [Tooltip("Постоянный уникальный ID Искры, совпадающий с ее записью прогресса.")]
        [SerializeField] private string _sparkId = SparkIds.First;
        [Tooltip("Коллайдеры Искры, активные в игровом мире и отключённые, пока Искра находится в руках.")]
        [SerializeField] private Collider[] _colliders;
        [Tooltip("Визуал этой Искры, предоставляющий точку крепления фрукта.")]
        [SerializeField] private SparkWorldVisual _worldVisual;

        [Header("Состояния")]
        [Tooltip("Длительность состояния Кушает без подключенной анимации.")]
        [SerializeField, Min(0f)] private float _eatingDuration = 1f;
        [Tooltip("Автоматически завершать состояние Кушает. Отключается после подключения Animation Event.")]
        [SerializeField] private bool _completeEatingAutomatically = true;

        private IPlayerHandsService _handsService;
        private ISparkProgressService _sparkProgressService;
        private IShelterInteractionRequestService _requestService;
        private ISparkLookTargets _lookTargets;

        public event Action<SparkBehaviourState> StateChanged;
        public event Action EatingAnimationCompleted;

        public string DisplayName => _sparkProgressService.GetProgress(_sparkId).DisplayName;
        public string Id => _sparkId;
        public HeldItemType ItemType => HeldItemType.Spark;
        public Transform ItemTransform => transform;
        public Transform FruitHoldAnchor => _worldVisual != null
            ? _worldVisual.FruitHoldAnchor
            : null;
        public SparkBehaviourState State { get; private set; } = SparkBehaviourState.Standing;
        public string InteractionPrompt =>
            _handsService.HeldItem is IFruitItem
                ? "Покормить Искру"
                : "Взять Искру";
        public bool CanInteract => _handsService.HeldItem is IFruitItem
            ? State != SparkBehaviourState.Eating
            : State != SparkBehaviourState.Held
              && State != SparkBehaviourState.Editing
              && !_handsService.HasItem;

        [Inject]
        private void Construct(IPlayerHandsService handsService, ISparkProgressService sparkProgressService,
            IShelterInteractionRequestService requestService, ISparkLookTargets lookTargets)
        {
            _handsService = handsService;
            _sparkProgressService = sparkProgressService;
            _requestService = requestService;
            _lookTargets = lookTargets;
            if (isActiveAndEnabled)
            {
                _lookTargets.RegisterSpark(this);
            }
        }

        private void Awake()
        {
            if (_colliders == null || _colliders.Length == 0)
            {
                _colliders = GetComponentsInChildren<Collider>(true);
            }

            ApplyState();
        }

        private void OnEnable()
        {
            _lookTargets?.RegisterSpark(this);
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
            TransitionTo(isHeld
                ? SparkBehaviourState.Held
                : SparkBehaviourState.Standing);
        }

        public bool TryStartMoving()
        {
            return TransitionTo(SparkBehaviourState.Moving);
        }

        public void CompleteMoving()
        {
            if (State == SparkBehaviourState.Moving)
            {
                TransitionTo(SparkBehaviourState.Standing);
            }
        }

        public bool TryStartResting() => TransitionTo(SparkBehaviourState.Resting);
        public void CompleteResting()
        {
            if (State == SparkBehaviourState.Resting)
            {
                TransitionTo(SparkBehaviourState.Standing);
            }
        }

        public bool TryStartEating()
        {
            if (!TransitionTo(SparkBehaviourState.Eating))
            {
                return false;
            }

            if (_completeEatingAutomatically)
            {
                CompleteEatingAfterDelayAsync().Forget();
            }

            return true;
        }

        public void CompleteEating()
        {
            if (State == SparkBehaviourState.Eating)
            {
                TransitionTo(SparkBehaviourState.Resting);
            }
        }

        public void CancelEating()
        {
            if (State == SparkBehaviourState.Eating)
            {
                TransitionTo(SparkBehaviourState.Standing);
            }
        }

        public bool BeginEditing()
        {
            return TransitionTo(SparkBehaviourState.Editing);
        }

        public void CompleteEatingFromAnimation()
        {
            if (State != SparkBehaviourState.Eating)
            {
                return;
            }

            EatingAnimationCompleted?.Invoke();
            CompleteEating();
        }

        private void OnDisable()
        {
            _lookTargets?.UnregisterSpark(this);
            CompleteEating();
        }

        private void OnDestroy()
        {
            _lookTargets?.UnregisterSpark(this);
            CompleteEating();
        }

        private async UniTask CompleteEatingAfterDelayAsync()
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_eatingDuration),
                    cancellationToken: this.GetCancellationTokenOnDestroy());
                CompleteEating();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private bool TransitionTo(SparkBehaviourState nextState)
        {
            if (State == nextState || !CanTransition(State, nextState))
            {
                return false;
            }

            State = nextState;
            ApplyState();
            StateChanged?.Invoke(State);
            return true;
        }

        private void ApplyState()
        {
            SetCollidersEnabled(State != SparkBehaviourState.Held);
        }

        private static bool CanTransition(SparkBehaviourState currentState, SparkBehaviourState nextState)
        {
            if (nextState == SparkBehaviourState.Held)
            {
                return currentState != SparkBehaviourState.Held;
            }

            if (nextState == SparkBehaviourState.Eating)
            {
                return currentState != SparkBehaviourState.Eating;
            }

            return currentState switch
            {
                SparkBehaviourState.Standing =>
                    nextState == SparkBehaviourState.Eating
                    || nextState == SparkBehaviourState.Moving
                    || nextState == SparkBehaviourState.Resting,
                SparkBehaviourState.Held =>
                    nextState == SparkBehaviourState.Standing
                    || nextState == SparkBehaviourState.Editing,
                SparkBehaviourState.Editing => false,
                SparkBehaviourState.Moving =>
                    nextState == SparkBehaviourState.Standing,
                SparkBehaviourState.Resting =>
                    nextState == SparkBehaviourState.Standing,
                SparkBehaviourState.Eating =>
                    nextState == SparkBehaviourState.Standing
                    || nextState == SparkBehaviourState.Resting,
                _ => false
            };
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
