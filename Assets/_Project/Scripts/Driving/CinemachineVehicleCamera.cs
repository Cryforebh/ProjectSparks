using Unity.Cinemachine;
using UnityEngine;

namespace ProjectSparks.Driving
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CinemachineCamera), typeof(CinemachineFollow))]
    public sealed class CinemachineVehicleCamera : MonoBehaviour
    {
        [Tooltip("Физическое тело машины, скорость которого влияет на камеру.")]
        [SerializeField] private Rigidbody _targetBody;
        [Tooltip("Базовое положение камеры относительно точки слежения.")]
        [SerializeField] private Vector3 _baseFollowOffset = new Vector3(0f, 4.7f, -7.2f);
        [Tooltip("Дополнительное отдаление камеры на высокой скорости.")]
        [SerializeField, Min(0f)] private float _speedDistance = 1.25f;
        [Tooltip("Скорость, при которой эффект камеры достигает максимума.")]
        [SerializeField, Min(0.1f)] private float _effectMaxSpeed = 14f;
        [Tooltip("Поле зрения неподвижной камеры.")]
        [SerializeField, Range(20f, 100f)] private float _baseFieldOfView = 58f;
        [Tooltip("Поле зрения камеры на высокой скорости.")]
        [SerializeField, Range(20f, 100f)] private float _speedFieldOfView = 64f;
        [Tooltip("Скорость изменения расстояния и поля зрения.")]
        [SerializeField, Min(0.01f)] private float _speedEffectResponse = 3.5f;

        private CinemachineCamera _cinemachineCamera;
        private CinemachineFollow _follow;

        private void Awake()
        {
            _cinemachineCamera = GetComponent<CinemachineCamera>();
            _follow = GetComponent<CinemachineFollow>();
            ApplySpeedEffect(true);
        }

        private void Update()
        {
            ApplySpeedEffect(false);
        }

        private void ApplySpeedEffect(bool immediate)
        {
            if (_cinemachineCamera == null || _follow == null)
            {
                return;
            }

            float speed = _targetBody == null ? 0f : _targetBody.linearVelocity.magnitude;
            float speed01 = Mathf.Clamp01(speed / Mathf.Max(_effectMaxSpeed, 0.1f));
            Vector3 desiredOffset = _baseFollowOffset + Vector3.back * (_speedDistance * speed01);
            float desiredFieldOfView = Mathf.Lerp(_baseFieldOfView, _speedFieldOfView, speed01);
            float blend = immediate
                ? 1f
                : 1f - Mathf.Exp(-_speedEffectResponse * Mathf.Max(Time.unscaledDeltaTime, 0.0001f));

            _follow.FollowOffset = Vector3.Lerp(_follow.FollowOffset, desiredOffset, blend);
            LensSettings lens = _cinemachineCamera.Lens;
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, desiredFieldOfView, blend);
            _cinemachineCamera.Lens = lens;
        }
    }
}
