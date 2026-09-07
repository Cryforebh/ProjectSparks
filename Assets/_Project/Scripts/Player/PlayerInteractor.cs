using System;
using ProjectSparks.Collectibles;
using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour, IInteractionPromptSource
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
        }

        private void Awake()
        {
            if (_viewCamera == null)
            {
                Debug.LogError("В PlayerInteractor не назначена камера.", this);
                enabled = false;
            }
        }

        private void Update()
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
                    TryDiscardHeldFruit();
                }
            }
        }

        private void OnDisable()
        {
            SetTarget(null);
            SetFocus(null);
        }

        private void RefreshTarget()
        {
            Ray ray = new Ray(
                _viewCamera.transform.position,
                _viewCamera.transform.forward);

            IInteractable target = null;
            IFocusable focus = null;
            if (Physics.SphereCast(
                    ray,
                    _interactionRadius,
                    out RaycastHit hit,
                    _interactionDistance,
                    _interactionMask,
                    QueryTriggerInteraction.Collide))
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
            else if ((_currentTarget != null || _currentFocus != null)
                && Time.unscaledTime - _lastFocusTime < _focusLostDelay)
            {
                return;
            }

            SetTarget(target);
            SetFocus(focus);
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
            return _handsService != null
                && _handsService.HeldItem is IFruitItem
                    ? "Выбросить фрукт"
                    : string.Empty;
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
