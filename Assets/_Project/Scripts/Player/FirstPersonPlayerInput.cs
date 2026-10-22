using ProjectSparks.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectSparks.Player
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class FirstPersonPlayerInput : MonoBehaviour, IPlayerInputSource, IPauseInputSource, IFrameUpdate
    {
        [Tooltip("Набор команд управления персонажем.")]
        [SerializeField] private InputActionAsset _inputActions;

        private InputActionMap _playerActions;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _interactAction;
        private InputAction _pauseAction;
        private bool _interactPressed;
        private bool _pausePressed;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }

        private void Awake()
        {
            if (_inputActions == null)
            {
                Debug.LogError("В FirstPersonPlayerInput не назначен набор Input Actions.", this);
                enabled = false;
                return;
            }

            _playerActions = _inputActions.FindActionMap("Player", true);
            _moveAction = _playerActions.FindAction("Move", true);
            _lookAction = _playerActions.FindAction("Look", true);
            _interactAction = _playerActions.FindAction("Interact", true);
            _pauseAction = _playerActions.FindAction("Pause", true);
        }

        private void OnEnable()
        {
            _playerActions?.Enable();
        }

        public void UpdateFrame()
        {
            Move = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
            Look = _lookAction.ReadValue<Vector2>();
            _interactPressed |= _interactAction.WasPressedThisFrame();
            _pausePressed |= _pauseAction.WasPressedThisFrame();
        }

        public bool ConsumeInteract()
        {
            bool value = _interactPressed;
            _interactPressed = false;
            return value;
        }

        public bool ConsumePause()
        {
            bool value = _pausePressed;
            _pausePressed = false;
            return value;
        }

        private void OnDisable()
        {
            _playerActions?.Disable();
            Move = Vector2.zero;
            Look = Vector2.zero;
            _interactPressed = false;
            _pausePressed = false;
        }
    }
}
