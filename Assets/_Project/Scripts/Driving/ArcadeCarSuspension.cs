using UnityEngine;

namespace ProjectSparks.Driving
{
    [RequireComponent(typeof(Rigidbody), typeof(ArcadeCarController))]
    public sealed class ArcadeCarSuspension : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Rigidbody машины, к которому прикладываются силы подвески.")]
        [SerializeField] private Rigidbody _body;
        [Tooltip("Точка подвески переднего левого колеса.")]
        [SerializeField] private Transform _frontLeftAnchor;
        [Tooltip("Точка подвески переднего правого колеса.")]
        [SerializeField] private Transform _frontRightAnchor;
        [Tooltip("Точка подвески заднего левого колеса.")]
        [SerializeField] private Transform _rearLeftAnchor;
        [Tooltip("Точка подвески заднего правого колеса.")]
        [SerializeField] private Transform _rearRightAnchor;

        [Header("Колеса")]
        [Tooltip("Визуальный объект переднего левого колеса.")]
        [SerializeField] private Transform _frontLeftWheel;
        [Tooltip("Визуальный объект переднего правого колеса.")]
        [SerializeField] private Transform _frontRightWheel;
        [Tooltip("Визуальный объект заднего левого колеса.")]
        [SerializeField] private Transform _rearLeftWheel;
        [Tooltip("Визуальный объект заднего правого колеса.")]
        [SerializeField] private Transform _rearRightWheel;

        [Header("Подвеска")]
        [Tooltip("Радиус сферической проверки колеса.")]
        [SerializeField, Min(0.05f)] private float _wheelRadius = 0.38f;
        [Tooltip("Рабочая длина подвески.")]
        [SerializeField, Min(0.05f)] private float _restDistance = 0.58f;
        [Tooltip("Максимальный ход колеса вверх.")]
        [SerializeField, Min(0f)] private float _maxCompression = 0.28f;
        [Tooltip("Максимальная высота уступа, который подвеска принимает как опору.")]
        [SerializeField, Min(0f)] private float _maxStepHeight = 0.38f;
        [Tooltip("Дополнительный ход колеса вниз при отрыве от поверхности.")]
        [SerializeField, Min(0f)] private float _maxDroop = 0.24f;
        [Tooltip("Жесткость одной пружины.")]
        [SerializeField, Min(0f)] private float _springStrength = 30000f;
        [Tooltip("Сила амортизатора, которая гасит раскачивание подвески.")]
        [SerializeField, Min(0f)] private float _damping = 3600f;
        [Tooltip("Ограничение максимальной силы одной точки подвески.")]
        [SerializeField, Min(0f)] private float _maxForcePerWheel = 12000f;
        [Tooltip("Слои, которые считаются поверхностью для подвески.")]
        [SerializeField] private LayerMask _groundMask = ~0;

        [Header("Столкновения")]
        [Tooltip("Отключать жесткие коллайдеры колес во время работы подвески.")]
        [SerializeField] private bool _disableRigidWheelColliders = true;

        [Header("Визуальный отклик")]
        [Tooltip("Скорость следования визуальных колес за поверхностью.")]
        [SerializeField, Min(0f)] private float _wheelVisualResponse = 16f;

        private readonly RaycastHit[] _groundHits = new RaycastHit[8];
        private SphereCollider[] _rigidWheelColliders;
        private bool[] _rigidWheelColliderStates;
        private float _frontLeftDistance;
        private float _frontRightDistance;
        private float _rearLeftDistance;
        private float _rearRightDistance;
        private int _groundedWheelCount;

        public int GroundedWheelCount => _groundedWheelCount;

        private void Awake()
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            CacheRigidWheelColliders();
            ResetProbeDistances();
        }

        private void OnEnable()
        {
            if (!_disableRigidWheelColliders || _rigidWheelColliders == null)
            {
                return;
            }

            foreach (SphereCollider wheelCollider in _rigidWheelColliders)
            {
                wheelCollider.enabled = false;
            }
        }

        private void OnDisable()
        {
            if (_rigidWheelColliders == null || _rigidWheelColliderStates == null)
            {
                return;
            }

            for (int index = 0; index < _rigidWheelColliders.Length; index++)
            {
                if (_rigidWheelColliders[index] != null)
                {
                    _rigidWheelColliders[index].enabled = _rigidWheelColliderStates[index];
                }
            }
        }

        private void FixedUpdate()
        {
            if (_body == null)
            {
                return;
            }

            _groundedWheelCount = 0;
            ApplySuspension(_frontLeftAnchor, ref _frontLeftDistance);
            ApplySuspension(_frontRightAnchor, ref _frontRightDistance);
            ApplySuspension(_rearLeftAnchor, ref _rearLeftDistance);
            ApplySuspension(_rearRightAnchor, ref _rearRightDistance);
        }

        private void LateUpdate()
        {
            float response = 1f - Mathf.Exp(-_wheelVisualResponse * Time.deltaTime);
            UpdateWheelVisual(_frontLeftWheel, _frontLeftAnchor, _frontLeftDistance, response);
            UpdateWheelVisual(_frontRightWheel, _frontRightAnchor, _frontRightDistance, response);
            UpdateWheelVisual(_rearLeftWheel, _rearLeftAnchor, _rearLeftDistance, response);
            UpdateWheelVisual(_rearRightWheel, _rearRightAnchor, _rearRightDistance, response);
        }

        private void ApplySuspension(Transform anchor, ref float currentDistance)
        {
            float maximumDistance = _restDistance + _maxDroop;
            float previousDistance = currentDistance;
            currentDistance = maximumDistance;
            if (anchor == null)
            {
                return;
            }

            float minimumDistance = Mathf.Max(0f, _restDistance - _maxCompression);
            float rayDistance = maximumDistance + _wheelRadius;
            Vector3 suspensionUp = transform.up;
            Vector3 direction = -suspensionUp;
            int hitCount = Physics.RaycastNonAlloc(
                anchor.position,
                direction,
                _groundHits,
                rayDistance,
                _groundMask,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = float.PositiveInfinity;
            bool blockedByHighStep = false;
            float suspensionVelocity = Vector3.Dot(_body.GetPointVelocity(anchor.position), suspensionUp);
            bool acceptingLanding = suspensionVelocity < -0.75f;
            bool suspensionAlreadyBottomed = previousDistance <= minimumDistance + 0.001f;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = _groundHits[index];
                if (hit.collider == null || hit.collider.attachedRigidbody == _body)
                {
                    continue;
                }

                float wheelCenterDistance = hit.distance - _wheelRadius;
                if (wheelCenterDistance >= nearestDistance)
                {
                    continue;
                }

                float requiredRise = previousDistance - wheelCenterDistance;
                if (requiredRise > _maxStepHeight && !acceptingLanding && !suspensionAlreadyBottomed)
                {
                    blockedByHighStep = true;
                    continue;
                }

                nearestDistance = wheelCenterDistance;
            }

            if (float.IsPositiveInfinity(nearestDistance))
            {
                if (blockedByHighStep)
                {
                    currentDistance = previousDistance;
                }

                return;
            }

            currentDistance = Mathf.Clamp(nearestDistance, minimumDistance, maximumDistance);
            _groundedWheelCount++;
            float compression = Mathf.Max(0f, _restDistance - currentDistance);
            if (compression <= 0f)
            {
                return;
            }

            float force = compression * _springStrength - suspensionVelocity * _damping;
            force = Mathf.Clamp(force, 0f, _maxForcePerWheel);
            _body.AddForceAtPosition(suspensionUp * force, anchor.position, ForceMode.Force);
        }

        private void UpdateWheelVisual(Transform wheel, Transform anchor, float distance, float response)
        {
            if (wheel == null || anchor == null)
            {
                return;
            }

            Vector3 targetPosition = anchor.position - transform.up * distance;
            float visualDistance = Vector3.Dot(anchor.position - wheel.position, transform.up);
            wheel.position = distance < visualDistance
                ? targetPosition
                : Vector3.Lerp(wheel.position, targetPosition, response);
        }

        private void ResetProbeDistances()
        {
            float maximumDistance = _restDistance + _maxDroop;
            _frontLeftDistance = maximumDistance;
            _frontRightDistance = maximumDistance;
            _rearLeftDistance = maximumDistance;
            _rearRightDistance = maximumDistance;
        }

        private void CacheRigidWheelColliders()
        {
            _rigidWheelColliders = GetComponents<SphereCollider>();
            _rigidWheelColliderStates = new bool[_rigidWheelColliders.Length];
            for (int index = 0; index < _rigidWheelColliders.Length; index++)
            {
                _rigidWheelColliderStates[index] = _rigidWheelColliders[index].enabled;
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawSuspensionGizmo(_frontLeftAnchor);
            DrawSuspensionGizmo(_frontRightAnchor);
            DrawSuspensionGizmo(_rearLeftAnchor);
            DrawSuspensionGizmo(_rearRightAnchor);
        }

        private void DrawSuspensionGizmo(Transform anchor)
        {
            if (anchor == null)
            {
                return;
            }

            Vector3 end = anchor.position - transform.up * (_restDistance + _maxDroop);
            Gizmos.color = new Color(0.1f, 0.85f, 0.9f, 0.9f);
            Gizmos.DrawLine(anchor.position, end);
            Gizmos.DrawWireSphere(end, _wheelRadius);
            Gizmos.color = new Color(1f, 0.72f, 0.12f, 0.9f);
            Gizmos.DrawWireSphere(anchor.position - transform.up * _restDistance, _wheelRadius);
            Gizmos.color = new Color(1f, 0.2f, 0.15f, 0.9f);
            float minimumDistance = Mathf.Max(0f, _restDistance - _maxCompression);
            Gizmos.DrawWireSphere(anchor.position - transform.up * minimumDistance, _wheelRadius);
        }
    }
}
