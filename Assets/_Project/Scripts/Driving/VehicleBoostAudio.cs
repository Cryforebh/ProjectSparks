using ProjectSparks.Core;
using UnityEngine;

namespace ProjectSparks.Driving
{
    /// <summary>
    /// Три отдельных звука ускорения: начало, непрерывная петля и завершение.
    /// Считывает только фактическое состояние Boost из ArcadeCarController.
    /// Не влияет на физику, управление и заряд ускорения.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleBoostAudio : MonoBehaviour, IFrameUpdate
    {
        [Header("Ссылки")]
        [Tooltip("Контроллер автомобиля. Должен иметь свойство IsBoostActive.")]
        [SerializeField] private ArcadeCarController _controller;

        [Tooltip("Отдельный AudioSource для однократного звука включения Boost.")]
        [SerializeField] private AudioSource _startSource;
        [Tooltip("Отдельный AudioSource для непрерывной петли Boost.")]
        [SerializeField] private AudioSource _loopSource;
        [Tooltip("Отдельный AudioSource для однократного звука выключения Boost.")]
        [SerializeField] private AudioSource _endSource;

        [Tooltip("Короткий звук начала ускорения.")]
        [SerializeField] private AudioClip _startClip;
        [Tooltip("Бесшовная петля для всей длительности ускорения.")]
        [SerializeField] private AudioClip _loopClip;
        [Tooltip("Короткий звук завершения ускорения.")]
        [SerializeField] private AudioClip _endClip;

        [Header("Громкость")]
        [Tooltip("Громкость короткого звука начала ускорения.")]
        [SerializeField, Range(0f, 1f)] private float _startVolume = 0.55f;
        [Tooltip("Максимальная громкость непрерывной петли ускорения.")]
        [SerializeField, Range(0f, 1f)] private float _loopVolume = 0.42f;
        [Tooltip("Громкость короткого звука завершения ускорения.")]
        [SerializeField, Range(0f, 1f)] private float _endVolume = 0.45f;

        [Header("Переходы")]
        [Tooltip("За сколько секунд петля Boost набирает полную громкость.")]
        [SerializeField, Min(0.01f)] private float _loopAttackTime = 0.18f;
        [Tooltip("За сколько секунд петля Boost плавно затихает после окончания ускорения.")]
        [SerializeField, Min(0.01f)] private float _loopReleaseTime = 0.22f;
        [Tooltip("Краткая задержка перед завершением Boost, чтобы звук не дрожал на неровностях.")]
        [SerializeField, Min(0f)] private float _endDetectionDelay = 0.05f;
        [Tooltip("Плавное приглушение предыдущего Boost_End при быстром повторном включении.")]
        [SerializeField, Min(0.01f)] private float _endInterruptionFadeTime = 0.10f;

        private bool _initialized;
        private bool _sessionActive;
        private bool _paused;
        private bool _fadingEnd;
        private float _lastActualBoostTime = float.NegativeInfinity;
        private float _currentLoopVolume;

        private void Reset()
        {
            _controller = GetComponentInParent<ArcadeCarController>();
        }

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponentInParent<ArcadeCarController>();
            }
        }

        private void Start()
        {
            if (!ValidateSetup())
            {
                enabled = false;
                return;
            }

            Configure(_startSource, _startClip, false, _startVolume);
            Configure(_loopSource, _loopClip, true, 0f);
            Configure(_endSource, _endClip, false, _endVolume);

            _initialized = true;
            ResetPlayback();
        }

        private bool ValidateSetup()
        {
            if (_controller == null ||
                _startSource == null || _loopSource == null || _endSource == null ||
                _startClip == null || _loopClip == null || _endClip == null ||
                _startSource == _loopSource || _startSource == _endSource || _loopSource == _endSource)
            {
                Debug.LogError(
                    "VehicleBoostAudio: назначьте ArcadeCarController, три РАЗНЫХ AudioSource и три AudioClip (Start, Loop, End).", this);
                return false;
            }

            return true;
        }

        private static void Configure(AudioSource source, AudioClip clip, bool loop, float volume)
        {
            source.Stop();
            source.playOnAwake = false;
            source.clip = clip;
            source.loop = loop;
            source.volume = volume;
            source.pitch = 1f;
        }

        private void OnEnable()
        {
            if (_initialized)
            {
                ResetPlayback();
            }
        }

        private void OnDisable()
        {
            if (_initialized)
            {
                ResetPlayback();
            }
        }

        private void ResetPlayback()
        {
            _startSource.Stop();
            _loopSource.Stop();
            _endSource.Stop();
            _startSource.volume = _startVolume;
            _loopSource.volume = 0f;
            _endSource.volume = _endVolume;
            _sessionActive = false;
            _paused = false;
            _fadingEnd = false;
            _currentLoopVolume = 0f;
            _lastActualBoostTime = float.NegativeInfinity;
        }

        public void UpdateFrame()
        {
            if (!_initialized)
            {
                return;
            }

            // Не проигрываем эффекты ускорения во время паузы игры.
            if (Time.timeScale <= 0f)
            {
                if (!_paused)
                {
                    _startSource.Pause();
                    _loopSource.Pause();
                    _endSource.Pause();
                    _paused = true;
                }

                return;
            }

            if (_paused)
            {
                _startSource.UnPause();
                _loopSource.UnPause();
                _endSource.UnPause();
                _paused = false;
            }

            bool actuallyBoosting = _controller.IsBoostActive;
            if (actuallyBoosting)
            {
                _lastActualBoostTime = Time.time;
                if (!_sessionActive)
                {
                    BeginBoost();
                }
            }
            else if (_sessionActive && Time.time - _lastActualBoostTime >= _endDetectionDelay)
            {
                FinishBoost();
            }

            UpdateLoopVolume(Time.deltaTime);
            UpdateEndInterruption(Time.deltaTime);
        }

        private void BeginBoost()
        {
            _sessionActive = true;

            // Если Boost перезапущен сразу после завершения, прежний End
            // приглушается, а не резко обрывается.
            if (_endSource.isPlaying)
            {
                _fadingEnd = true;
            }

            _startSource.volume = _startVolume;
            _startSource.Play();

            // Петля начинается тихо и плавно входит под звук Start.
            // Если её хвост ещё затухает после прошлого Boost, продолжаем
            // использовать тот же источник без повторного запуска.
            if (!_loopSource.isPlaying)
            {
                _loopSource.volume = 0f;
                _currentLoopVolume = 0f;
                _loopSource.Play();
            }
        }

        private void FinishBoost()
        {
            _sessionActive = false;

            // Начальный короткий эффект не обрываем: пусть доиграет.
            // Петля уйдёт по _loopReleaseTime, а End играет один раз.
            _fadingEnd = false;
            _endSource.volume = _endVolume;
            _endSource.Play();
        }

        private void UpdateLoopVolume(float deltaTime)
        {
            float target = _sessionActive ? _loopVolume : 0f;
            float time = _sessionActive ? _loopAttackTime : _loopReleaseTime;
            float changePerSecond = Mathf.Max(0.001f, _loopVolume) / Mathf.Max(0.01f, time);
            _currentLoopVolume = Mathf.MoveTowards(
                _currentLoopVolume,
                target,
                changePerSecond * deltaTime);

            _loopSource.volume = _currentLoopVolume;

            if (!_sessionActive && _currentLoopVolume <= 0.0001f && _loopSource.isPlaying)
            {
                _loopSource.Stop();
                _currentLoopVolume = 0f;
            }
        }

        private void UpdateEndInterruption(float deltaTime)
        {
            if (!_fadingEnd)
            {
                return;
            }

            float step = Mathf.Max(0.001f, _endVolume) * deltaTime /
                Mathf.Max(0.01f, _endInterruptionFadeTime);
            _endSource.volume = Mathf.MoveTowards(_endSource.volume, 0f, step);
            if (_endSource.volume <= 0.0001f || !_endSource.isPlaying)
            {
                _endSource.Stop();
                _endSource.volume = _endVolume;
                _fadingEnd = false;
            }
        }
    }
}
