using ProjectSparks.Driving;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class DrivingHudView : MonoBehaviour
    {
        [Tooltip("Текст текущей скорости машины.")]
        [SerializeField] private TMP_Text _speedText;
        [Tooltip("Текст оставшегося заряда Boost.")]
        [SerializeField] private TMP_Text _boostText;
        [Tooltip("Заполняемое изображение шкалы Boost.")]
        [SerializeField] private Image _boostFill;

        private IVehicleTelemetry _telemetry;
        private int _lastSpeed = -1;
        private int _lastBoost = -1;

        [Inject]
        private void Construct(IVehicleTelemetry telemetry)
        {
            _telemetry = telemetry;
        }

        private void Start()
        {
            RefreshDisplay(true);
        }

        private void LateUpdate()
        {
            RefreshDisplay(false);
        }

        private void RefreshDisplay(bool force)
        {
            int speed = Mathf.RoundToInt(_telemetry.SpeedKph);
            int boost = Mathf.RoundToInt(_telemetry.Boost01 * 100f);

            if (force || speed != _lastSpeed)
            {
                if (_speedText != null)
                {
                    _speedText.text = $"Скорость: {speed} км/ч";
                }

                _lastSpeed = speed;
            }

            if (force || boost != _lastBoost)
            {
                if (_boostText != null)
                {
                    _boostText.text = $"Boost: {boost}%";
                }

                if (_boostFill != null)
                {
                    _boostFill.fillAmount = Mathf.Clamp01(_telemetry.Boost01);
                }

                _lastBoost = boost;
            }
        }
    }
}
