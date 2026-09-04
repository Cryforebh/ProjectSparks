using System;
using UnityEngine;

namespace ProjectSparks.Driving
{
    [RequireComponent(typeof(ArcadeCarController))]
    public sealed class ArcadeCarVisuals : MonoBehaviour
    {
        [Tooltip("Контроллер машины, из которого берутся скорость и текущий угол руления.")]
        [SerializeField] private ArcadeCarController _controller;
        [Tooltip("Корневой объект визуального кузова, который наклоняется при повороте.")]
        [SerializeField] private Transform _bodyVisual;
        [Tooltip("Дополнительные объекты, которые повторяют крен кузова, например закрепленные коробки.")]
        [SerializeField] private Transform[] _bodyRollFollowers = new Transform[0];
        [Tooltip("Общая точка вращения кузова и закрепленного груза.")]
        [SerializeField] private Transform _bodyRollPivot;
        [Tooltip("Визуальный объект переднего левого колеса.")]
        [SerializeField] private Transform _wheelFrontLeft;
        [Tooltip("Визуальный объект переднего правого колеса.")]
        [SerializeField] private Transform _wheelFrontRight;
        [Tooltip("Визуальный объект заднего левого колеса.")]
        [SerializeField] private Transform _wheelRearLeft;
        [Tooltip("Визуальный объект заднего правого колеса.")]
        [SerializeField] private Transform _wheelRearRight;
        [Tooltip("Радиус колеса. Влияет только на скорость визуального вращения.")]
        [SerializeField, Min(0.05f)] private float _wheelRadius = 0.38f;
        [Tooltip("Максимальный визуальный угол передних колес на малой скорости.")]
        [SerializeField, Range(0f, 45f)] private float _maxVisualSteer = 24f;
        [Tooltip("Максимальный визуальный угол передних колес на высокой скорости.")]
        [SerializeField, Range(0f, 45f)] private float _highSpeedVisualSteer = 8f;
        [Tooltip("Максимальный визуальный наклон кузова при повороте.")]
        [SerializeField, Range(0f, 10f)] private float _maxBodyRoll = 3.5f;

        private Quaternion _bodyInitialRotation;
        private Quaternion _frontLeftInitialRotation;
        private Quaternion _frontRightInitialRotation;
        private Quaternion _rearLeftInitialRotation;
        private Quaternion _rearRightInitialRotation;
        private Transform _bodyRollSpace;
        private Vector3 _bodyRollPivotInitialPosition;
        private Vector3[] _bodyRollFollowerInitialPositions = new Vector3[0];
        private Quaternion[] _bodyRollFollowerInitialRotations = new Quaternion[0];
        private float _spinAngle;

        private void Awake()
        {
            CaptureInitialRotations();
        }

        private void LateUpdate()
        {
            if (_controller == null)
            {
                return;
            }

            _spinAngle += _controller.ForwardSpeed / Mathf.Max(_wheelRadius, 0.05f) * Mathf.Rad2Deg * Time.deltaTime;
            float availableVisualAngle = Mathf.Lerp(
                _maxVisualSteer,
                _highSpeedVisualSteer,
                _controller.Speed01);
            float steerAngle = _controller.SmoothedSteer * availableVisualAngle;

            ApplyWheel(_wheelFrontLeft, _frontLeftInitialRotation, _spinAngle, steerAngle);
            ApplyWheel(_wheelFrontRight, _frontRightInitialRotation, _spinAngle, steerAngle);
            ApplyWheel(_wheelRearLeft, _rearLeftInitialRotation, _spinAngle, 0f);
            ApplyWheel(_wheelRearRight, _rearRightInitialRotation, _spinAngle, 0f);

            if (_bodyVisual == null)
            {
                return;
            }

            float speed01 = Mathf.Clamp01(Mathf.Abs(_controller.ForwardSpeed) / _controller.MaxForwardSpeed);
            float roll = -_controller.SmoothedSteer * _maxBodyRoll * speed01;
            Quaternion rollRotation = Quaternion.AngleAxis(roll, Vector3.forward);
            Quaternion targetBodyRotation = _bodyInitialRotation * rollRotation;
            _bodyVisual.localRotation = targetBodyRotation;
            Quaternion bodyRollDelta = targetBodyRotation * Quaternion.Inverse(_bodyInitialRotation);
            ApplyBodyRollFollowers(bodyRollDelta);
        }

        private void CaptureInitialRotations()
        {
            ResolveBodyRollFollowers();
            if (_bodyVisual != null) _bodyInitialRotation = _bodyVisual.localRotation;
            if (_wheelFrontLeft != null) _frontLeftInitialRotation = _wheelFrontLeft.localRotation;
            if (_wheelFrontRight != null) _frontRightInitialRotation = _wheelFrontRight.localRotation;
            if (_wheelRearLeft != null) _rearLeftInitialRotation = _wheelRearLeft.localRotation;
            if (_wheelRearRight != null) _rearRightInitialRotation = _wheelRearRight.localRotation;

            _bodyRollSpace = _bodyVisual != null && _bodyVisual.parent != null
                ? _bodyVisual.parent
                : transform;
            Transform pivot = _bodyRollPivot != null ? _bodyRollPivot : _bodyVisual;
            _bodyRollPivotInitialPosition = pivot != null
                ? _bodyRollSpace.InverseTransformPoint(pivot.position)
                : Vector3.zero;

            _bodyRollFollowerInitialPositions = new Vector3[_bodyRollFollowers.Length];
            _bodyRollFollowerInitialRotations = new Quaternion[_bodyRollFollowers.Length];
            for (int index = 0; index < _bodyRollFollowers.Length; index++)
            {
                Transform follower = _bodyRollFollowers[index];
                if (follower == null)
                {
                    _bodyRollFollowerInitialPositions[index] = Vector3.zero;
                    _bodyRollFollowerInitialRotations[index] = Quaternion.identity;
                    continue;
                }

                _bodyRollFollowerInitialPositions[index] = _bodyRollSpace.InverseTransformPoint(follower.position);
                _bodyRollFollowerInitialRotations[index] =
                    Quaternion.Inverse(_bodyRollSpace.rotation) * follower.rotation;
            }
        }

        private void ResolveBodyRollFollowers()
        {
            if (_bodyRollFollowers != null && _bodyRollFollowers.Length > 0)
            {
                return;
            }

            Transform[] children = GetComponentsInChildren<Transform>(true);
            int followerCount = 0;
            foreach (Transform child in children)
            {
                if (child.name.StartsWith("CargoVisual_", StringComparison.Ordinal))
                {
                    followerCount++;
                }
            }

            _bodyRollFollowers = new Transform[followerCount];
            int followerIndex = 0;
            foreach (Transform child in children)
            {
                if (child.name.StartsWith("CargoVisual_", StringComparison.Ordinal))
                {
                    _bodyRollFollowers[followerIndex++] = child;
                }
            }
        }

        private void ApplyBodyRollFollowers(Quaternion rollRotation)
        {
            int followerCount = Mathf.Min(
                _bodyRollFollowers.Length,
                Mathf.Min(
                    _bodyRollFollowerInitialPositions.Length,
                    _bodyRollFollowerInitialRotations.Length));
            for (int index = 0; index < followerCount; index++)
            {
                Transform follower = _bodyRollFollowers[index];
                if (follower == null ||
                    _bodyRollSpace == null ||
                    follower == _bodyVisual ||
                    follower.IsChildOf(_bodyVisual))
                {
                    continue;
                }

                Vector3 initialOffset =
                    _bodyRollFollowerInitialPositions[index] - _bodyRollPivotInitialPosition;
                Vector3 targetPosition =
                    _bodyRollPivotInitialPosition + rollRotation * initialOffset;
                follower.position = _bodyRollSpace.TransformPoint(targetPosition);
                follower.rotation =
                    _bodyRollSpace.rotation * rollRotation * _bodyRollFollowerInitialRotations[index];
            }
        }

        private static void ApplyWheel(Transform wheel, Quaternion initialRotation, float spin, float steer)
        {
            if (wheel == null)
            {
                return;
            }

            Quaternion steerRotation = Quaternion.AngleAxis(steer, Vector3.up);
            Quaternion spinRotation = Quaternion.AngleAxis(spin, Vector3.right);
            wheel.localRotation = initialRotation * steerRotation * spinRotation;
        }
    }
}
