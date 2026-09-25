using UnityEngine;

namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    public sealed class SparkGrowthView : MonoBehaviour
    {
        [Tooltip("Калибровка изменения пропорций Искры.")]
        [SerializeField] private SparkGrowthCalibration _calibration;

        [Tooltip("Корень FBX-модели Искры.")]
        [SerializeField] private Transform _model;

        [Tooltip("Время плавного изменения внешности.")]
        [SerializeField, Min(0.01f)]
        private float _transitionDuration = 0.6f;

        [Tooltip("Размер усиков полностью маленькой Искры.")]
        [SerializeField, Range(0.1f, 1f)]
        private float _antennaBabyScale = 0.35f;

        private SparkGrowthBinding _binding;

        private float _currentGrowth;
        private float _startGrowth;
        private float _targetGrowth;

        private float _currentEarDevelopment;
        private float _startEarDevelopment;
        private float _targetEarDevelopment;

        private float _transitionTime;
        private bool _initialized;

        private void Awake()
        {
            if (_calibration != null && _model != null)
            {
                _binding = _calibration.Bind(_model);
            }
        }

        public void SetDevelopment(float growth, float earDevelopment)
        {
            growth = Mathf.Clamp01(growth);
            earDevelopment = Mathf.Clamp01(earDevelopment);

            if (!_initialized)
            {
                _currentGrowth = growth;
                _startGrowth = growth;
                _targetGrowth = growth;

                _currentEarDevelopment = earDevelopment;
                _startEarDevelopment = earDevelopment;
                _targetEarDevelopment = earDevelopment;

                _initialized = true;
                return;
            }

            if (Mathf.Approximately(_targetGrowth, growth)
                && Mathf.Approximately(
                    _targetEarDevelopment,
                    earDevelopment))
            {
                return;
            }

            _startGrowth = _currentGrowth;
            _targetGrowth = growth;

            _startEarDevelopment = _currentEarDevelopment;
            _targetEarDevelopment = earDevelopment;

            _transitionTime = 0f;
        }

        private void LateUpdate()
        {
            if (!_initialized || _binding == null)
            {
                return;
            }

            bool changing =!Mathf.Approximately(_currentGrowth, _targetGrowth)
                || !Mathf.Approximately(_currentEarDevelopment, _targetEarDevelopment);

            if (changing)
            {
                _transitionTime += Time.deltaTime;

                float t = Mathf.Clamp01(_transitionTime / _transitionDuration);

                // SmoothStep.
                t = t * t * (3f - 2f * t);

                _currentGrowth = Mathf.Lerp(_startGrowth, _targetGrowth, t);

                _currentEarDevelopment = Mathf.Lerp(_startEarDevelopment, _targetEarDevelopment, t);

                if (_transitionTime >= _transitionDuration)
                {
                    _currentGrowth = _targetGrowth;
                    _currentEarDevelopment = _targetEarDevelopment;
                }
            }

            _binding.Apply(
                _currentGrowth,

                // Размер ушей.
                _currentEarDevelopment,

                // Завал ушей развивается параллельно их размеру.
                _currentEarDevelopment,

                _antennaBabyScale);
        }
    }
}
