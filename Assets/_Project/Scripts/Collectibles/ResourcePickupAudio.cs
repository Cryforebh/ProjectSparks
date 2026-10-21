using UnityEngine;

namespace ProjectSparks.Collectibles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourcePickupSensor))]
    public sealed class ResourcePickupAudio : MonoBehaviour
    {
        [Tooltip("Отдельный AudioSource звука подбора. Может находиться на другом объекте машины, например Sounds/ResourcePickupAudio.")]
        [SerializeField] private AudioSource _source;
        [Tooltip("Короткий звук успешного подбора фрукта. Воспроизводится на машине, независимо от выключения подобранного объекта.")]
        [SerializeField] private AudioClip _fruitClip;
        [Tooltip("Громкость звука подбора фрукта.")]
        [SerializeField, Range(0f, 1f)] private float _volume = 0.55f;
        [Tooltip("Случайное отклонение высоты тона от единицы при каждом воспроизведении.")]
        [SerializeField, Range(0f, 0.2f)] private float _pitchVariation = 0.04f;
        [Min(0f), Tooltip("Минимальный интервал между звуками подбора, в секундах. Пока предыдущий звук играет, следующий не запускается.")]
        [SerializeField] private float _minimumInterval = 0.08f;

        private ResourcePickupSensor _sensor;
        private float _nextPlayTime;

        private void Awake()
        {
            _sensor = GetComponent<ResourcePickupSensor>();
            if (_source == null)
            {
                Debug.LogError("В ResourcePickupAudio назначьте AudioSource звука подбора.", this);
                enabled = false;
                return;
            }
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.dopplerLevel = 0f;
        }

        private void OnEnable() => _sensor.Collected += HandleCollected;

        private void OnDisable()
        {
            _sensor.Collected -= HandleCollected;
            if (_source != null)
                _source.Stop();
            _nextPlayTime = 0f;
        }

        private void HandleCollected(ResourceType resourceType)
        {
            if (resourceType != ResourceType.Fruit || _fruitClip == null || _source == null ||
                Time.timeScale <= 0f || Time.time < _nextPlayTime || _source.isPlaying)
                return;

            _source.clip = _fruitClip;
            _source.volume = _volume;
            _source.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
            _source.Play();
            _nextPlayTime = Time.time + _minimumInterval;
        }
    }
}
