using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerFootsteps : MonoBehaviour, IFrameUpdate
    {
        [Tooltip("Контроллер, который сообщает фактически пройденное расстояние и состояние ходьбы.")]
        [SerializeField] private FirstPersonController _controller;
        [Tooltip("Отдельный источник звуков шагов персонажа.")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("Варианты одиночного шага. Один и тот же вариант не выбирается два раза подряд.")]
        [SerializeField] private AudioClip[] _clips;
        [Min(0.1f), Tooltip("Расстояние между шагами в метрах. Первый шаг звучит после половины этого расстояния.")]
        [SerializeField] private float _stepDistance = 1.6f;
        [Range(0f, 1f), Tooltip("Основная громкость звука шага.")]
        [SerializeField] private float _volume = 0.25f;
        [Range(0f, 0.5f), Tooltip("Случайное уменьшение громкости каждого шага: 0.1 означает до 10 процентов.")]
        [SerializeField] private float _volumeVariation = 0.1f;
        [Range(0f, 0.2f), Tooltip("Случайное отклонение высоты тона шага от обычного значения 1.")]
        [SerializeField] private float _pitchVariation = 0.035f;

        private IPauseService _pauseService;
        private IPlayerControlService _controlService;
        private int _lastStep;
        private int _lastClip = -1;

        [Inject]
        private void Construct(IPauseService pauseService, IPlayerControlService controlService)
        {
            _pauseService = pauseService;
            _controlService = controlService;
        }

        private void Awake()
        {
            if (_controller == null || _audioSource == null || _clips == null || _clips.Length == 0)
            {
                Debug.LogError("В PlayerFootsteps назначьте контроллер, AudioSource и звуки шагов.", this);
                enabled = false;
                return;
            }

            foreach (AudioClip clip in _clips)
            {
                if (clip == null)
                {
                    Debug.LogError("В PlayerFootsteps есть пустой элемент списка звуков.", this);
                    enabled = false;
                    return;
                }
            }

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = 1f;
        }

        private void OnEnable()
        {
            if (_controller != null)
            {
                _lastStep = GetStepNumber();
            }
        }

        public void UpdateFrame()
        {
            int step = GetStepNumber();
            bool stepChanged = step != _lastStep;
            _lastStep = step;

            if (_pauseService.IsPaused || !_controlService.IsControlEnabled || !_controller.isActiveAndEnabled)
            {
                _audioSource.Stop();
                return;
            }

            if (!_controller.IsWalking || !stepChanged)
            {
                return;
            }

            int clipIndex = Random.Range(0, _clips.Length);
            if (_clips.Length > 1 && _lastClip >= 0)
            {
                clipIndex = Random.Range(0, _clips.Length - 1);
                if (clipIndex >= _lastClip)
                {
                    clipIndex++;
                }
            }

            _lastClip = clipIndex;
            _audioSource.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
            float volume = _volume * Random.Range(1f - _volumeVariation, 1f);
            _audioSource.PlayOneShot(_clips[clipIndex], volume);
        }

        private int GetStepNumber()
        {
            return Mathf.FloorToInt(_controller.WalkedDistance / _stepDistance + 0.5f);
        }

        private void OnDisable()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }
    }
}
