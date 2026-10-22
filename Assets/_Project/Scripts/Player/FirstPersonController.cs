using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour, IFrameUpdate
    {
        [Header("Перемещение")]
        [Tooltip("Скорость перемещения персонажа.")]
        [SerializeField, Min(0.1f)] private float _moveSpeed = 4f;
        [Tooltip("Ускорение свободного падения персонажа.")]
        [SerializeField] private float _gravity = -19.62f;

        [Header("Обзор")]
        [Tooltip("Точка вертикального вращения камеры.")]
        [SerializeField] private Transform _cameraPivot;
        [Tooltip("Чувствительность обзора мышью.")]
        [SerializeField, Min(0.01f)] private float _lookSensitivity = 0.12f;
        [Tooltip("Максимальный угол обзора вверх и вниз.")]
        [SerializeField, Range(1f, 89f)] private float _verticalLookLimit = 80f;

        private CharacterController _characterController;
        private IPlayerInputSource _input;
        private IPauseService _pauseService;
        private IPlayerControlService _controlService;
        private float _verticalVelocity;
        private float _pitch;

        [Inject]
        private void Construct(
            IPlayerInputSource input,
            IPauseService pauseService,
            IPlayerControlService controlService)
        {
            _input = input;
            _pauseService = pauseService;
            _controlService = controlService;
        }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_cameraPivot == null)
            {
                Debug.LogError("В FirstPersonController не назначена точка камеры.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            _pauseService.PauseChanged += HandlePauseChanged;
            _controlService.ControlEnabledChanged += HandleControlEnabledChanged;
            RefreshCursorState();
        }

        public void UpdateFrame()
        {
            if (_pauseService.IsPaused || !_controlService.IsControlEnabled)
            {
                return;
            }

            ApplyLook();
            ApplyMovement();
        }

        private void OnDestroy()
        {
            if (_pauseService != null)
            {
                _pauseService.PauseChanged -= HandlePauseChanged;
            }

            if (_controlService != null)
            {
                _controlService.ControlEnabledChanged -= HandleControlEnabledChanged;
            }

            SetCursorLocked(false);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _pauseService != null)
            {
                RefreshCursorState();
            }
        }

        private void ApplyLook()
        {
            Vector2 look = _input.Look * _lookSensitivity;
            transform.Rotate(0f, look.x, 0f, Space.Self);

            _pitch = Mathf.Clamp(
                _pitch - look.y,
                -_verticalLookLimit,
                _verticalLookLimit);
            _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void ApplyMovement()
        {
            Vector2 moveInput = _input.Move;
            Vector3 planarMovement =
                transform.right * moveInput.x
                + transform.forward * moveInput.y;

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += _gravity * Time.deltaTime;
            Vector3 velocity =
                planarMovement * _moveSpeed
                + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);
        }

        private void HandlePauseChanged(bool isPaused)
        {
            RefreshCursorState();
        }

        private void HandleControlEnabledChanged(bool isEnabled)
        {
            RefreshCursorState();
        }

        private void RefreshCursorState()
        {
            bool shouldLockCursor =
                !_pauseService.IsPaused && _controlService.IsControlEnabled;
            SetCursorLocked(shouldLockCursor);
        }

        private static void SetCursorLocked(bool isLocked)
        {
            Cursor.lockState = isLocked
                ? CursorLockMode.Locked
                : CursorLockMode.None;
            Cursor.visible = !isLocked;
        }
    }
}
