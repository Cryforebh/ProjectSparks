using ProjectSparks.Collectibles;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class SparkFruitAttachment : MonoBehaviour
    {
        [Tooltip("Точка крепления фрукта на визуальной модели Искры.")]
        [SerializeField] private Transform _anchor;

        [Tooltip("Метка на GrowthBody, за которой следует точка крепления без наследования масштаба.")]
        [SerializeField] private Transform _growthGuide;

        [Header("Обхват фрукта")]
        [SerializeField] private Rig _gripRig;
        [SerializeField] private RigBuilder _rigBuilder;
        [SerializeField] private Transform _leftTarget;
        [SerializeField] private Transform _rightTarget;
        [Tooltip("Время плавного возврата лап после еды.")]
        [SerializeField, Min(0.01f)] private float _releaseDuration = 0.15f;

        private PhysicalFruitItem _fruit;
        private SparkInteractable _spark;
        private float _releaseStartWeight;
        private float _releaseElapsed;
        private bool _isReleasing;

        public Transform Anchor => _anchor;

        private void Awake()
        {
            if (_anchor == null || _growthGuide == null || _gripRig == null
                || _rigBuilder == null || _leftTarget == null || _rightTarget == null)
            {
                Debug.LogError("Не настроены якорь или риг удержания фрукта.", this);
                enabled = false;
                return;
            }

            // Сохраняем выставленное вручную положение якоря для взрослой Искры.
            _growthGuide.SetPositionAndRotation(_anchor.position, _anchor.rotation);
            _spark = GetComponentInParent<SparkInteractable>();
            _gripRig.weight = 0f;
        }

        public void SetFruit(PhysicalFruitItem fruit)
        {
            _fruit = fruit;
            bool hasTargets = isActiveAndEnabled && fruit != null
                && fruit.LeftHandTarget != null && fruit.RightHandTarget != null;
            if (hasTargets)
            {
                _isReleasing = false;
                UpdateTargets();
                _gripRig.weight = 1f;
                _rigBuilder.SyncLayers();
            }
            else if (_gripRig != null && !_isReleasing && _gripRig.weight > 0f)
            {
                _releaseStartWeight = _gripRig.weight;
                _releaseElapsed = 0f;
                _isReleasing = true;
            }
        }

        private void Update()
        {
            if (_fruit != null && (!_fruit.transform.IsChildOf(_anchor)
                || (_spark != null && _spark.State != SparkBehaviourState.Eating)))
            {
                SetFruit(null);
            }

            if (_fruit != null && _gripRig != null && !_isReleasing)
            {
                UpdateTargets();
                _rigBuilder.SyncLayers();
            }

            if (_isReleasing)
            {
                _releaseElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_releaseElapsed / _releaseDuration);
                float eased = t * t * (3f - 2f * t);
                _gripRig.weight = Mathf.Lerp(_releaseStartWeight, 0f, eased);
                _rigBuilder.SyncLayers();
                if (t >= 1f) _isReleasing = false;
            }
        }

        private void LateUpdate()
        {
            _anchor.SetPositionAndRotation(_growthGuide.position, _growthGuide.rotation);
        }

        private void OnDisable()
        {
            _fruit = null;
            _isReleasing = false;
            if (_gripRig != null) _gripRig.weight = 0f;
        }

        private void UpdateTargets()
        {
            _leftTarget.SetPositionAndRotation(_fruit.LeftHandTarget.position, _fruit.LeftHandTarget.rotation);
            _rightTarget.SetPositionAndRotation(_fruit.RightHandTarget.position, _fruit.RightHandTarget.rotation);
        }

    }
}
