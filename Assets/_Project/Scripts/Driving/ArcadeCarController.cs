using ProjectSparks.Companions;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Driving
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour, IVehicleTelemetry
    {
        private const float _centerOfMassGizmoRadius = 0.14f;
        private const float _centerOfMassAxisLength = 0.32f;

        [Header("Скорость")]
        [Tooltip("Максимальная скорость движения вперед в метрах в секунду.")]
        [SerializeField, Min(1f)] private float _maxForwardSpeed = 14f;
        [Tooltip("Максимальная скорость заднего хода в метрах в секунду.")]
        [SerializeField, Min(1f)] private float _maxReverseSpeed = 5.5f;
        [Tooltip("Ускорение машины при нажатии газа.")]
        [SerializeField, Min(0f)] private float _forwardAcceleration = 18f;
        [Tooltip("Ускорение при движении задним ходом.")]
        [SerializeField, Min(0f)] private float _reverseAcceleration = 10f;
        [Tooltip("Сила отдельного тормоза.")]
        [SerializeField, Min(0f)] private float _brakingAcceleration = 30f;
        [Tooltip("Плавное замедление машины, когда газ и тормоз отпущены.")]
        [SerializeField, Min(0f)] private float _rollingResistance = 1.6f;

        [Header("Поворот и сцепление")]
        [Tooltip("Максимальная скорость поворота машины на малой скорости.")]
        [SerializeField, Min(0f)] private float _lowSpeedSteering = 105f;
        [Tooltip("Максимальная скорость поворота машины около предельной скорости.")]
        [SerializeField, Min(0f)] private float _highSpeedSteering = 45f;
        [Tooltip("Скорость сглаживания ввода руля. Большее значение делает реакцию резче.")]
        [SerializeField, Min(0.01f)] private float _steeringInputResponse = 6.5f;
        [Tooltip("Насколько быстро фактическая угловая скорость приближается к требуемой.")]
        [SerializeField, Min(0f)] private float _steeringResponse = 7.5f;
        [Tooltip("Ограничение углового ускорения при повороте. Уменьшите, если машина дергается.")]
        [SerializeField, Min(0f)] private float _maxSteeringAcceleration = 10f;
        [Tooltip("Сила подавления бокового скольжения. Большее значение дает более цепкое управление.")]
        [SerializeField, Min(0f)] private float _lateralGrip = 7.5f;
        [Tooltip("Максимальное ускорение, которым контроллер может гасить боковое скольжение.")]
        [SerializeField, Min(0f)] private float _maxGripAcceleration = 34f;
        [Tooltip("Дополнительное прижатие машины к поверхности, возрастающее со скоростью.")]
        [SerializeField, Min(0f)] private float _downforce;

        [Header("Стабильность")]
        [Tooltip("Локальное положение центра массы относительно корневого объекта машины.")]
        [SerializeField] private Vector3 _centerOfMass = new Vector3(0f, 0.38f, 0f);
        [Tooltip("Сила, возвращающая накренившуюся машину в вертикальное положение.")]
        [SerializeField, Min(0f)] private float _uprightStrength = 15f;
        [Tooltip("Гашение раскачивания кузова вокруг продольной и поперечной осей.")]
        [SerializeField, Min(0f)] private float _uprightDamping = 4f;
        [Tooltip("Высота начала луча проверки поверхности относительно корня машины.")]
        [SerializeField, Min(0f)] private float _groundProbeHeight = 0.65f;
        [Tooltip("Длина луча вниз, на которой машина считается находящейся на поверхности.")]
        [SerializeField, Min(0.1f)] private float _groundProbeDistance = 1.4f;
        [Tooltip("Слои физики, которые могут считаться поверхностью под машиной.")]
        [SerializeField] private LayerMask _groundMask = ~0;

        [Header("Ускорение")]
        [Tooltip("Дополнительное ускорение при удержании кнопки Boost")]
        [SerializeField, Min(0f)] private float _boostAcceleration = 11f;
        [Tooltip("Время непрерывной работы Boost при полном заряде, в секундах.")]
        [SerializeField, Min(0.1f)] private float _boostDuration = 1.8f;
        [Tooltip("Время полного восстановления заряда Boost, в секундах.")]
        [SerializeField, Min(0.01f)] private float _boostRechargeTime = 3.5f;

        private readonly RaycastHit[] _groundHits = new RaycastHit[8];
        private Rigidbody _body;
        private IVehicleInputSource _input;
        private ICompanionService _companionService;
        private Vector3 _lastSafePosition;
        private Quaternion _lastSafeRotation;
        private float _nextSafePoseTime;
        private float _boostCharge = 1f;
        private bool _grounded;
        private Vector3 _groundNormal = Vector3.up;
        private float _smoothedSteer;

        public Rigidbody Body => _body;
        public Vector3 Position => transform.position;
        public float SpeedKph => _body == null ? 0f : _body.linearVelocity.magnitude * 3.6f;
        public float ForwardSpeed => _body == null ? 0f : Vector3.Dot(_body.linearVelocity, transform.forward);
        public float MaxForwardSpeed => _maxForwardSpeed;
        public float SmoothedSteer => _smoothedSteer;
        public float Speed01 => Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / Mathf.Max(_maxForwardSpeed, 0.01f));
        public float Boost01 => _boostCharge;
        public bool IsGrounded => _grounded;

        [Inject]
        private void Construct(
            IVehicleInputSource inputSource,
            ICompanionService companionService)
        {
            _input = inputSource;
            _companionService = companionService;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.centerOfMass = _centerOfMass;
            _body.maxAngularVelocity = 6f;
            _lastSafePosition = transform.position;
            _lastSafeRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        private void Update()
        {
            bool resetRequested = _input.ConsumeReset();
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (resetRequested || transform.position.y < -5f)
            {
                Recover();
            }
        }

        private void FixedUpdate()
        {
            _smoothedSteer = Mathf.MoveTowards(
                _smoothedSteer,
                _input.Steer,
                _steeringInputResponse * Time.fixedDeltaTime);

            ProbeGround();
            ApplyUprightStability();

            if (!_grounded)
            {
                RechargeBoost();
                return;
            }

            ApplyDrive();
            ApplyLateralGrip();
            ApplySteering();
            ApplyDownforce();
            UpdateSafePose();
        }

        public void Recover()
        {
            transform.SetPositionAndRotation(_lastSafePosition + Vector3.up * 0.55f, _lastSafeRotation);
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _smoothedSteer = 0f;
            _body.WakeUp();
        }

        private void ProbeGround()
        {
            Vector3 origin = transform.position + Vector3.up * _groundProbeHeight;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                _groundHits,
                _groundProbeDistance,
                _groundMask,
                QueryTriggerInteraction.Ignore);

            _grounded = false;
            _groundNormal = Vector3.up;
            float nearestDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = _groundHits[index];
                if (hit.collider == null || hit.collider.attachedRigidbody == _body || hit.distance >= nearestDistance)
                {
                    continue;
                }

                _grounded = true;
                nearestDistance = hit.distance;
                _groundNormal = hit.normal;
            }
        }

        private void ApplyDrive()
        {
            float drive = _input.Drive;
            float forwardSpeed = ForwardSpeed;

            _body.linearDamping = Mathf.Abs(drive) < 0.01f && !_input.BrakeHeld ? 0.32f : 0.08f;

            if (_input.BrakeHeld)
            {
                ApplyBrake();
                RechargeBoost();
                return;
            }

            if (drive > 0.01f)
            {
                if (forwardSpeed < _maxForwardSpeed)
                {
                    _body.AddForce(transform.forward * _forwardAcceleration * drive, ForceMode.Acceleration);
                }
            }
            else if (drive < -0.01f)
            {
                float amount = -drive;
                if (forwardSpeed > -_maxReverseSpeed)
                {
                    _body.AddForce(-transform.forward * _reverseAcceleration * amount, ForceMode.Acceleration);
                }
            }
            else
            {
                float resistance = Mathf.Clamp(forwardSpeed, -1f, 1f) * _rollingResistance;
                _body.AddForce(-transform.forward * resistance, ForceMode.Acceleration);
            }

            if (_input.BoostHeld && drive > 0.1f && _boostCharge > 0f)
            {
                _body.AddForce(transform.forward * _boostAcceleration, ForceMode.Acceleration);
                float baseConsumption = Time.fixedDeltaTime / _boostDuration;
                float consumption = _companionService.ModifyBoostConsumption(baseConsumption);
                _boostCharge = Mathf.Max(0f, _boostCharge - consumption);
            }
            else
            {
                RechargeBoost();
            }
        }

        private void ApplyBrake()
        {
            Vector3 planarVelocity = Vector3.ProjectOnPlane(_body.linearVelocity, _groundNormal);
            if (planarVelocity.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 targetVelocity = Vector3.MoveTowards(
                planarVelocity,
                Vector3.zero,
                _brakingAcceleration * Time.fixedDeltaTime);
            Vector3 brakingAccelerationVector = (targetVelocity - planarVelocity) / Time.fixedDeltaTime;
            _body.AddForce(brakingAccelerationVector, ForceMode.Acceleration);
        }

        private void RechargeBoost()
        {
            _boostCharge = Mathf.Min(1f, _boostCharge + Time.fixedDeltaTime / _boostRechargeTime);
        }

        private void ApplyLateralGrip()
        {
            float lateralSpeed = Vector3.Dot(_body.linearVelocity, transform.right);
            float correction = Mathf.Clamp(-lateralSpeed * _lateralGrip, -_maxGripAcceleration, _maxGripAcceleration);
            _body.AddForce(transform.right * correction, ForceMode.Acceleration);
        }

        private void ApplySteering()
        {
            float speed = Mathf.Abs(ForwardSpeed);
            float steerAvailability = Mathf.InverseLerp(0.15f, 1.4f, speed);
            float speed01 = Mathf.InverseLerp(0f, _maxForwardSpeed, speed);
            float steerRate = Mathf.Lerp(_lowSpeedSteering, _highSpeedSteering, speed01);
            float reverseSign = ForwardSpeed < -0.2f ? -1f : 1f;
            float targetYawSpeed = _smoothedSteer * steerRate * steerAvailability * reverseSign * Mathf.Deg2Rad;
            float currentYawSpeed = Vector3.Dot(_body.angularVelocity, _groundNormal);
            float responseDelta = (targetYawSpeed - currentYawSpeed) * _steeringResponse * Time.fixedDeltaTime;
            float maxDelta = _maxSteeringAcceleration * Time.fixedDeltaTime;
            float newYawSpeed = currentYawSpeed + Mathf.Clamp(responseDelta, -maxDelta, maxDelta);
            _body.angularVelocity += _groundNormal * (newYawSpeed - currentYawSpeed);
        }

        private void ApplyDownforce()
        {
            float speed = _body.linearVelocity.magnitude;
            _body.AddForce(-_groundNormal * _downforce * speed, ForceMode.Acceleration);
        }

        private void ApplyUprightStability()
        {
            Vector3 correctionAxis = Vector3.Cross(transform.up, Vector3.up);
            Vector3 angularDamping = Vector3.ProjectOnPlane(_body.angularVelocity, Vector3.up);
            _body.AddTorque(
                correctionAxis * _uprightStrength - angularDamping * _uprightDamping,
                ForceMode.Acceleration);
        }

        private void UpdateSafePose()
        {
            if (Time.time < _nextSafePoseTime || Vector3.Dot(transform.up, Vector3.up) < 0.78f)
            {
                return;
            }

            _nextSafePoseTime = Time.time + 0.3f;
            _lastSafePosition = transform.position;
            _lastSafeRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        private void OnDrawGizmosSelected()
        {
            DrawCenterOfMassGizmo();

            Gizmos.color = _grounded ? Color.green : Color.red;
            Vector3 origin = transform.position + Vector3.up * _groundProbeHeight;
            Gizmos.DrawLine(origin, origin + Vector3.down * _groundProbeDistance);
        }

        private void DrawCenterOfMassGizmo()
        {
            Vector3 center = Application.isPlaying && _body != null
                ? _body.worldCenterOfMass
                : transform.TransformPoint(_centerOfMass);

            Gizmos.color = new Color(1f, 0.62f, 0.08f, 0.9f);
            Gizmos.DrawSphere(center, _centerOfMassGizmoRadius * 0.55f);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(center, _centerOfMassGizmoRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                center - transform.right * _centerOfMassAxisLength,
                center + transform.right * _centerOfMassAxisLength);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                center - transform.up * _centerOfMassAxisLength,
                center + transform.up * _centerOfMassAxisLength);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                center - transform.forward * _centerOfMassAxisLength,
                center + transform.forward * _centerOfMassAxisLength);
        }
    }
}
