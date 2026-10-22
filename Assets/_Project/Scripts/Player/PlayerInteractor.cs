using System;
using ProjectSparks.Collectibles;
using ProjectSparks.Companions;
using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour, IInteractionPromptSource, IFrameUpdate
    {
        [Tooltip("Камера, из центра которой проверяется взаимодействие.")]
        [SerializeField] private Camera _viewCamera;
        [Tooltip("Максимальная дистанция взаимодействия.")]
        [SerializeField, Min(0.1f)] private float _interactionDistance = 2.5f;
        [Tooltip("Радиус области наведения вокруг центра экрана.")]
        [SerializeField, Min(0f)] private float _interactionRadius = 0.08f;
        [Tooltip("Время удержания фокуса после краткой потери цели.")]
        [SerializeField, Min(0f)] private float _focusLostDelay = 0.1f;
        [Tooltip("Слои объектов, доступных для взаимодействия.")]
        [SerializeField] private LayerMask _interactionMask = ~0;

        private IPlayerInputSource _input;
        private IPauseService _pauseService;
        private IPlayerControlService _controlService;
        private IPlayerHandsService _handsService;
        private IFruitWorldService _fruitWorldService;
        private IInteractable _currentTarget;
        private IFocusable _currentFocus;
        private float _lastFocusTime;
        private RaycastHit[] _interactionHits = new RaycastHit[16];

        public event Action<string> PromptChanged;

        public string CurrentPrompt { get; private set; } = string.Empty;

        [Inject]
        private void Construct(
            IPlayerInputSource input,
            IPauseService pauseService,
            IPlayerControlService controlService,
            IPlayerHandsService handsService,
            IFruitWorldService fruitWorldService)
        {
            _input = input;
            _pauseService = pauseService;
            _controlService = controlService;
            _handsService = handsService;
            _fruitWorldService = fruitWorldService;
            _handsService.HeldItemChanged += HandleHeldItemChanged;
        }

        private void Awake()
        {
            if (_viewCamera == null)
            {
                Debug.LogError("В PlayerInteractor не назначена камера.", this);
                enabled = false;
            }
        }

        public void UpdateFrame()
        {
            if (_pauseService.IsPaused || !_controlService.IsControlEnabled)
            {
                SetTarget(null);
                SetFocus(null);
                _input.ConsumeInteract();
                return;
            }

            RefreshTarget();

            if (_input.ConsumeInteract())
            {
                if (_currentTarget != null)
                {
                    _currentTarget.Interact();
                }
                else
                {
                    if (_handsService.HeldItem is SparkInteractable)
                    {
                        TryPutDownHeldSpark();
                    }
                    else
                    {
                        TryDiscardHeldFruit();
                    }
                }
            }
        }

        private void OnDisable()
        {
            SetTarget(null);
            SetFocus(null);
        }

        private void OnDestroy()
        {
            if (_handsService != null)
            {
                _handsService.HeldItemChanged -= HandleHeldItemChanged;
            }
        }

        private void HandleHeldItemChanged(IHoldableItem item)
        {
            if (item == null)
            {
                return;
            }

            if (_currentTarget is Component target && target.transform.IsChildOf(item.ItemTransform))
            {
                SetTarget(null);
            }

            if (_currentFocus is Component focus && focus.transform.IsChildOf(item.ItemTransform))
            {
                SetFocus(null);
            }
        }

        private void RefreshTarget()
        {
            Ray ray = new Ray(
                _viewCamera.transform.position,
                _viewCamera.transform.forward);

            IInteractable target = null;
            IFocusable focus = null;
            bool hasHit = TryGetTargetHit(ray, out RaycastHit hit);
            if (hasHit)
            {
                target = hit.collider.GetComponentInParent<IInteractable>();
                focus = hit.collider.GetComponentInParent<IFocusable>();
                if (target != null && !target.CanInteract)
                {
                    target = null;
                }
            }

            if (target != null || focus != null)
            {
                _lastFocusTime = Time.unscaledTime;
            }
            else if (!hasHit && (_currentTarget != null || _currentFocus != null)
                && Time.unscaledTime - _lastFocusTime < _focusLostDelay)
            {
                return;
            }

            SetTarget(target);
            SetFocus(focus);
        }

        private bool TryGetTargetHit(Ray ray, out RaycastHit closestHit)
        {
            int hitCount = Physics.SphereCastNonAlloc(ray, _interactionRadius, _interactionHits,
                _interactionDistance, _interactionMask, QueryTriggerInteraction.Collide);

            while (hitCount == _interactionHits.Length)
            {
                Array.Resize(ref _interactionHits, _interactionHits.Length * 2);
                hitCount = Physics.SphereCastNonAlloc(ray, _interactionRadius, _interactionHits,
                    _interactionDistance, _interactionMask, QueryTriggerInteraction.Collide);
            }

            Transform heldTransform = _handsService.HeldItem?.ItemTransform;
            float closestDistance = float.PositiveInfinity;
            closestHit = default;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = _interactionHits[i];
                Transform hitTransform = candidate.collider.transform;
                if (hitTransform.IsChildOf(transform)
                    || (heldTransform != null && hitTransform.IsChildOf(heldTransform)))
                {
                    continue;
                }

                if (candidate.distance < closestDistance)
                {
                    closestDistance = candidate.distance;
                    closestHit = candidate;
                    found = true;
                }
            }

            return found;
        }

        private void SetTarget(IInteractable target)
        {
            string prompt = target?.InteractionPrompt
                ?? GetHeldItemPrompt();
            if (ReferenceEquals(_currentTarget, target)
                && CurrentPrompt == prompt)
            {
                return;
            }

            _currentTarget = target;
            CurrentPrompt = prompt;
            PromptChanged?.Invoke(CurrentPrompt);
        }

        private string GetHeldItemPrompt()
        {
            if (_handsService?.HeldItem is IFruitItem)
                return "Выбросить фрукт";
            if (_handsService?.HeldItem is SparkInteractable)
                return "Поставить Искру";
            return string.Empty;
        }

        private void TryPutDownHeldSpark()
        {
            Vector3 candidate = transform.position + transform.forward * 1.25f;
            Vector3 origin = candidate + Vector3.up * 1.5f;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 3f,
                _interactionMask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.normal.y < 0.75f
                    || Mathf.Abs(hit.point.y - transform.position.y) > 0.5f
                    || hit.collider.transform.IsChildOf(transform)
                    || (_handsService.HeldItem?.ItemTransform != null
                        && hit.collider.transform.IsChildOf(_handsService.HeldItem.ItemTransform)))
                    continue;

                Vector3 position = hit.point;
                Collider[] nearby = Physics.OverlapSphere(position + Vector3.up * 0.3f,
                    0.25f, _interactionMask, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                foreach (Collider collider in nearby)
                {
                    if (collider == hit.collider
                        || collider.transform.IsChildOf(transform)
                        || collider.transform.IsChildOf(_handsService.HeldItem.ItemTransform))
                        continue;
                    blocked = true;
                    break;
                }

                if (!blocked)
                {
                    _handsService.TryPutDownSpark(position,
                        Quaternion.Euler(0f, transform.eulerAngles.y + 180f, 0f));
                    return;
                }
            }
        }

        private void TryDiscardHeldFruit()
        {
            if (_handsService.HeldItem is not IFruitItem)
            {
                return;
            }

            _fruitWorldService.TryDropHeldFruit();
        }

        private void SetFocus(IFocusable focus)
        {
            if (ReferenceEquals(_currentFocus, focus))
            {
                return;
            }

            _currentFocus?.SetFocused(false);
            _currentFocus = focus;
            _currentFocus?.SetFocused(true);
        }
    }
}
