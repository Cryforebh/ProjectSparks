using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectSparks.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionTargetMarker : MonoBehaviour
    {
        [Tooltip("Группа колец на поверхности зоны.")]
        [SerializeField] private Transform _rings;
        [Tooltip("Точка иконки. Локальная высота задает отступ вверх от центра колец относительно камеры.")]
        [SerializeField] private Transform _parcel;
        [Tooltip("Дочерний спрайт в центре точки. Только он поворачивается к камере.")]
        [SerializeField] private SpriteRenderer _parcelIcon;
        [Tooltip("Относительное изменение размера колец при пульсации.")]
        [SerializeField, Range(0f, 0.2f)] private float _pulseAmount = 0.05f;
        [Tooltip("Число плавных пульсаций в секунду.")]
        [SerializeField, Min(0f)] private float _pulseFrequency = 0.65f;
        [Tooltip("Амплитуда покачивания посылки по высоте в метрах.")]
        [SerializeField, Min(0f)] private float _floatHeight = 0.16f;

        private Vector3 _ringScale;
        private float _parcelHeight;
        private Camera _viewCamera;
        private Sprite _placeholderSprite;
        private float _elapsed;

        private void Awake()
        {
            if (_rings == null || _parcel == null || _parcelIcon == null)
            {
                Debug.LogError("В MissionTargetMarker назначьте кольца и иконку посылки.", this);
                enabled = false;
                return;
            }

            _ringScale = _rings.localScale;
            _parcelHeight = _parcel.localPosition.y;
            _viewCamera = Camera.main;
            if (_parcelIcon.sprite == null)
            {
                Texture2D texture = Texture2D.whiteTexture;
                _placeholderSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), texture.width);
                _placeholderSprite.name = "ParcelPlaceholder";
                _parcelIcon.sprite = _placeholderSprite;
            }
        }

        private void OnEnable()
        {
            _elapsed = 0f;
            if (_rings != null && _parcel != null)
                Animate();
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            Animate();
        }

        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (_viewCamera == null)
                _viewCamera = Camera.main;
            if (camera != _viewCamera || _viewCamera == null)
                return;

            float wave = Mathf.Sin(_elapsed * _pulseFrequency * Mathf.PI * 2f);
            // Одинаковая глубина относительно камеры сохраняет экранное положение по горизонтали.
            _parcel.position = _rings.position + camera.transform.up * (_parcelHeight + wave * _floatHeight);
            _parcelIcon.transform.rotation = camera.transform.rotation;
        }

        private void OnDestroy()
        {
            if (_placeholderSprite != null)
                Destroy(_placeholderSprite);
        }

        private void Animate()
        {
            float wave = Mathf.Sin(_elapsed * _pulseFrequency * Mathf.PI * 2f);
            _rings.localScale = _ringScale * (1f + wave * _pulseAmount);
        }
    }
}
