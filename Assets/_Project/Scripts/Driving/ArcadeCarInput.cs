using ProjectSparks.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectSparks.Driving
{
    [DefaultExecutionOrder(-100)]
    public sealed class ArcadeCarInput : MonoBehaviour, IVehicleInputSource, IPauseInputSource, IFrameUpdate
    {
        [Tooltip("Набор команд управления машиной.")]
        [SerializeField] private InputActionAsset _inputActions;

        private InputActionMap _vehicleActions;
        private InputAction _steerAction;
        private InputAction _accelerateAction;
        private InputAction _brakeReverseAction;
        private InputAction _brakeAction;
        private InputAction _boostAction;
        private InputAction _resetVehicleAction;
        private InputAction _pauseAction;
        private bool _resetPressed;
        private bool _pausePressed;

        public float Steer { get; private set; }
        public float Drive { get; private set; }
        public bool BrakeHeld { get; private set; }
        public bool BoostHeld { get; private set; }

        private void Awake()
        {
            if (_inputActions == null)
            {
                Debug.LogError("В ArcadeCarInput не назначен набор Input Actions.", this);
                enabled = false;
                return;
            }

            _vehicleActions = _inputActions.FindActionMap("Vehicle", true);
            _steerAction = _vehicleActions.FindAction("Steer", true);
            _accelerateAction = _vehicleActions.FindAction("Accelerate", true);
            _brakeReverseAction = _vehicleActions.FindAction("BrakeReverse", true);
            _brakeAction = _vehicleActions.FindAction("Brake", true);
            _boostAction = _vehicleActions.FindAction("Boost", true);
            _resetVehicleAction = _vehicleActions.FindAction("ResetVehicle", true);
            _pauseAction = _vehicleActions.FindAction("Pause", true);
        }

        private void OnEnable()
        {
            _vehicleActions?.Enable();
        }

        public void UpdateFrame()
        {
            Steer = Mathf.Clamp(_steerAction.ReadValue<float>(), -1f, 1f);
            Drive = Mathf.Clamp(
                _accelerateAction.ReadValue<float>() - _brakeReverseAction.ReadValue<float>(),
                -1f,
                1f);
            BrakeHeld = _brakeAction.IsPressed();
            BoostHeld = _boostAction.IsPressed();
            _resetPressed |= _resetVehicleAction.WasPressedThisFrame();
            _pausePressed |= _pauseAction.WasPressedThisFrame();
        }

        public bool ConsumeReset()
        {
            bool value = _resetPressed;
            _resetPressed = false;
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
            _vehicleActions?.Disable();
            Steer = 0f;
            Drive = 0f;
            BrakeHeld = false;
            BoostHeld = false;
            _resetPressed = false;
            _pausePressed = false;
        }
    }
}
