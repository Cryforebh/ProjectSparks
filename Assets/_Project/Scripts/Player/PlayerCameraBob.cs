using ProjectSparks.Core;
using UnityEngine;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerCameraBob : MonoBehaviour, IFrameUpdate
    {
        [Tooltip("Контроллер, который сообщает фактически пройденное расстояние и состояние ходьбы.")]
        [SerializeField] private FirstPersonController _controller;
        [Tooltip("Transform камеры, смещаемый относительно исходной позиции. Вращение мышью остается на CameraPivot.")]
        [SerializeField] private Transform _camera;
        [Min(0.2f), Tooltip("Расстояние полного цикла левого и правого шага в метрах. Для синхронизации равно двум расстояниям шага в PlayerFootsteps.")]
        [SerializeField] private float _strideLength = 3.2f;
        [Min(0f), Tooltip("Максимальное вертикальное смещение камеры от исходной позиции в локальных метрах.")]
        [SerializeField] private float _verticalAmplitude = 0.01f;
        [Min(0f), Tooltip("Максимальное боковое смещение камеры от исходной позиции в локальных метрах.")]
        [SerializeField] private float _horizontalAmplitude = 0.004f;
        [Min(0.1f), Tooltip("Скорость плавного выхода на покачивание и возврата камеры при остановке. Большее значение дает более быстрый переход.")]
        [SerializeField] private float _response = 10f;

        private Vector3 _basePosition;
        private Vector3 _offset;
        private bool _initialized;

        private void Awake()
        {
            if (_controller == null || _camera == null)
            {
                Debug.LogError("В PlayerCameraBob назначьте контроллер и Transform камеры.", this);
                enabled = false;
                return;
            }

            _basePosition = _camera.localPosition;
            _initialized = true;
        }

        public void UpdateFrame()
        {
            Vector3 targetOffset = Vector3.zero;
            if (_controller.isActiveAndEnabled && _controller.IsWalking)
            {
                float phase = _controller.WalkedDistance / _strideLength * Mathf.PI * 2f;
                targetOffset.x = Mathf.Sin(phase) * _horizontalAmplitude;
                targetOffset.y = Mathf.Cos(phase * 2f) * _verticalAmplitude;
            }

            float blend = 1f - Mathf.Exp(-_response * Time.deltaTime);
            _offset = Vector3.Lerp(_offset, targetOffset, blend);
            _camera.localPosition = _basePosition + _offset;
        }

        private void OnDisable()
        {
            if (_initialized && _camera != null)
            {
                _camera.localPosition = _basePosition;
                _offset = Vector3.zero;
            }
        }
    }
}
