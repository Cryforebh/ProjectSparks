using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Driving
{
    /// <summary>
    /// Звук двигателя, реакция на Boost и полёт. Не меняет Rigidbody, физику или ввод машины.
    /// Две петли воспроизводятся одновременно и плавно смешиваются по скорости и газу.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleEngineAudio : MonoBehaviour, IFrameUpdate
    {
        [Header("Ссылки")]
        [Tooltip("Контроллер автомобиля, откуда берутся фактическая скорость и ее предел.")]
        [SerializeField] private ArcadeCarController _controller;
        [Tooltip("AudioSource для холостого хода. Используйте отдельный источник.")]
        [SerializeField] private AudioSource _idleSource;
        [Tooltip("AudioSource для двигателя под нагрузкой. Используйте отдельный источник.")]
        [SerializeField] private AudioSource _driveSource;
        [SerializeField] private AudioClip _idleClip;
        [SerializeField] private AudioClip _driveClip;

        [Header("Громкость")]
        [Tooltip("Максимальная громкость холостого хода.")]
        [SerializeField, Range(0f, 1f)] private float _idleVolume = 0.55f;
        [Tooltip("Максимальная громкость двигателя под нагрузкой.")]
        [SerializeField, Range(0f, 1f)] private float _driveVolume = 0.62f;

        [Header("Высота тона (Pitch)")]
        [SerializeField, Range(0.5f, 2f)] private float _idlePitchMin = 0.98f;
        [SerializeField, Range(0.5f, 2f)] private float _idlePitchMax = 1.08f;
        [SerializeField, Range(0.5f, 2f)] private float _drivePitchMin = 0.90f;
        [SerializeField, Range(0.5f, 2f)] private float _drivePitchMax = 1.50f;
        [Tooltip("Повышение Pitch от газа на неподвижной машине. При разгоне Pitch продолжает расти до максимальной скорости.")]
        [SerializeField, Range(0f, 1f)] private float _throttlePitchInfluence = 0.35f;
        [Tooltip("Какая часть скоростного повышения Pitch остается при отпущенном газе.")]
        [SerializeField, Range(0f, 1f)] private float _coastPitchFactor = 0.65f;

        [Header("Смешивание петель")]
        [Tooltip("Какой уровень Drive дает полностью нажатый газ на неподвижной машине.")]
        [SerializeField, Range(0f, 1f)] private float _stationaryThrottleBlend = 0.52f;
        [Tooltip("Какая часть скоростного слоя Drive остается при движении по инерции.")]
        [SerializeField, Range(0f, 1f)] private float _coastDriveFactor = 0.60f;
        [Tooltip("Зависимость слоя Drive от доли максимальной скорости машины.")]
        [SerializeField] private AnimationCurve _driveBlendBySpeed =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Скорость перехода между звучаниями. Чем больше, тем быстрее отклик.")]
        [SerializeField, Min(0.1f)] private float _response = 5f;

        [Header("Boost и сверхскорость")]
        [Tooltip("На сколько усиливается Pitch двигателя во время реального Boost: 0.06 = +6%.")]
        [SerializeField, Range(0f, 0.25f)] private float _boostPitchBonus = 0.06f;
        [Tooltip("Насколько увеличивается громкость нагруженного двигателя при Boost: 0.08 = +8%.")]
        [SerializeField, Range(0f, 0.25f)] private float _boostVolumeBonus = 0.08f;
        [Tooltip("Время плавного включения звуковой реакции Boost, в секундах.")]
        [SerializeField, Min(0.01f)] private float _boostAttackTime = 0.18f;
        [Tooltip("Время плавного выключения звуковой реакции Boost, в секундах.")]
        [SerializeField, Min(0.01f)] private float _boostReleaseTime = 0.4f;
        [Tooltip("Скорость сверх обычного предела (м/с), на которой будет достигнута вся надбавка Pitch.")]
        [SerializeField, Min(0.1f)] private float _overSpeedRange = 20f;
        [Tooltip("Дополнительное повышение Pitch при движении быстрее обычного предела: 0.08 = +8%.")]
        [SerializeField, Range(0f, 0.3f)] private float _overSpeedPitchBonus = 0.08f;

        [Header("Звучание в воздухе")]
        [Tooltip("Доля нагруженного звучания Drive, которая остается во время полета (0 = только Idle, 1 = без изменения).")]
        [SerializeField, Range(0f, 1f)] private float _airborneDriveVolumeMultiplier = 0.35f;
        [Tooltip("Дополнительное снижение Pitch двигателя в воздухе. 0.90 = тон на 10% ниже.")]
        [SerializeField, Range(0.7f, 1f)] private float _airbornePitchMultiplier = 0.90f;
        [Tooltip("Сколько тона двигателя остается в воздухе при полностью нажатом газе. При отпущенном газе тон стремится к холостому ходу.")]
        [SerializeField, Range(0f, 1f)] private float _airborneThrottleTone = 0.55f;
        [Tooltip("Сколько секунд машина должна непрерывно находиться без контакта с землей, прежде чем включится воздушное звучание. Отсекает краткие подскоки.")]
        [SerializeField, Min(0f)] private float _airborneEnterDelay = 0.12f;
        [Tooltip("Время перехода к воздушному звучанию, в секундах.")]
        [SerializeField, Min(0.01f)] private float _airborneTransitionTime = 0.25f;
        [Tooltip("Время возвращения нагрузки на двигатель после приземления, в секундах.")]
        [SerializeField, Min(0.01f)] private float _landingTransitionTime = 0.20f;

        private IVehicleInputSource _input;
        private bool _initialized;
        private float _blend;
        private float _tone;
        private float _master;
        private float _boostBlend;
        private float _airborneBlend;
        private float _ungroundedDuration;

        [Inject]
        private void Construct(IVehicleInputSource inputSource)
        {
            _input = inputSource;
        }

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
            if (_controller == null ||
                _idleSource == null || _driveSource == null ||
                _idleSource == _driveSource ||
                _idleClip == null || _driveClip == null)
            {
                Debug.LogError(
                    "VehicleEngineAudio: назначьте контроллер, два разных AudioSource и две звуковые петли.", this);
                enabled = false;
                return;
            }

            if (_input == null)
            {
                Debug.LogWarning(
                    "VehicleEngineAudio: IVehicleInputSource не внедрен через Zenject. " +
                    "Звук будет реагировать на скорость, но не на газ и тормоз.", this);
            }

            PrepareSource(_idleSource, _idleClip);
            PrepareSource(_driveSource, _driveClip);
            _initialized = true;
            StartLoops();
        }

        private static void PrepareSource(AudioSource source, AudioClip clip)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.clip = clip;
            source.volume = 0f;
        }

        private void OnEnable()
        {
            // При первом включении Start еще не вызывался.
            if (_initialized)
            {
                StartLoops();
            }
        }

        private void OnDisable()
        {
            if (!_initialized)
            {
                return;
            }

            _idleSource.Stop();
            _driveSource.Stop();
        }

        private void StartLoops()
        {
            _idleSource.volume = 0f;
            _driveSource.volume = 0f;
            _idleSource.pitch = _idlePitchMin;
            _driveSource.pitch = _drivePitchMin;
            _blend = 0f;
            _tone = 0f;
            _master = 0f;
            _boostBlend = 0f;
            _airborneBlend = 0f;
            _ungroundedDuration = 0f;

            // И idle, и drive работают все время. Мы меняем лишь их громкость и Pitch.
            _idleSource.Play();
            _driveSource.Play();
        }

        public void UpdateFrame()
        {
            if (!_initialized)
            {
                return;
            }

            bool paused = Time.timeScale <= 0f;
            bool braking = _input != null && _input.BrakeHeld;
            float throttle = paused || braking || _input == null
                ? 0f
                : Mathf.Clamp01(Mathf.Abs(_input.Drive));

            // При движении назад скорость тоже положительна для аудиорасчета.
            // Берем фактический MaxForwardSpeed из Inspector автомобиля.
            float speed01 = Mathf.Clamp01(
                Mathf.Abs(_controller.ForwardSpeed) / Mathf.Max(0.01f, _controller.MaxForwardSpeed));

            // Учитываем уже вычисленное контроллером состояние земли.
            // Пока машина подпрыгивает на мелких неровностях, нагрузку не сбрасываем.
            if (!paused)
            {
                _ungroundedDuration = _controller.IsGrounded
                    ? 0f
                    : _ungroundedDuration + Time.deltaTime;

                float airborneTarget = _ungroundedDuration >= _airborneEnterDelay &&
                    !_controller.IsGrounded ? 1f : 0f;
                float airborneTransition = airborneTarget > _airborneBlend
                    ? _airborneTransitionTime
                    : _landingTransitionTime;
                _airborneBlend = Mathf.MoveTowards(
                    _airborneBlend,
                    airborneTarget,
                    Time.deltaTime / Mathf.Max(0.01f, airborneTransition));
            }

            float speedBlend = _driveBlendBySpeed == null
                ? speed01
                : Mathf.Clamp01(_driveBlendBySpeed.Evaluate(speed01));

            float speedLoad = speedBlend * Mathf.Lerp(_coastDriveFactor, 1f, throttle);
            float targetBlend = Mathf.Clamp01(Mathf.Max(
                speedLoad, throttle * _stationaryThrottleBlend));

            // В полёте снимаем именно ощущение нагрузки, а не глушим двигатель.
            // Освободившуюся долю микса занимает Idle.
            float airborneLoad = Mathf.Lerp(1f, _airborneDriveVolumeMultiplier, _airborneBlend);
            targetBlend *= airborneLoad;

            // Газ повышает тон на низкой скорости, но не «съедает»
            // оставшийся диапазон Pitch до достижения максимальной скорости.
            float coastTone = speed01 * _coastPitchFactor;
            float poweredTone = Mathf.Lerp(_throttlePitchInfluence, 1f, speed01);
            float groundTone = Mathf.Lerp(coastTone, poweredTone, throttle);
            // В полёте скорость машины больше не должна изображать нагрузку на мотор:
            // без газа тон уходит к Idle, с газом слышен более лёгкий свободный набор оборотов.
            float airTone = throttle * _airborneThrottleTone;
            float targetTone = Mathf.Lerp(groundTone, airTone, _airborneBlend);

            // Экспоненциальное сглаживание с одинаковой реакцией при разном FPS.
            float step = 1f - Mathf.Exp(-Mathf.Max(0.1f, _response) * Time.unscaledDeltaTime);
            _blend = Mathf.Lerp(_blend, targetBlend, step);
            _tone = Mathf.Lerp(_tone, targetTone, step);
            _master = Mathf.Lerp(_master, paused ? 0f : 1f, step);

            // Только действующий Boost, а не удерживаемая кнопка:
            // сила Boost отмечается самим физическим контроллером.
            bool boostActive = !paused && _controller.IsBoostActive;
            float boostFadeTime = boostActive ? _boostAttackTime : _boostReleaseTime;
            _boostBlend = Mathf.MoveTowards(
                _boostBlend,
                boostActive ? 1f : 0f,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, boostFadeTime));

            // Скоростной Pitch плавно продолжается выше обычного предела.
            // Это работает и после отпускания Boost, пока машина движется быстро.
            float overSpeed = Mathf.Max(0f,
                Mathf.Abs(_controller.ForwardSpeed) - _controller.MaxForwardSpeed);
            float overSpeed01 = Mathf.Clamp01(overSpeed / Mathf.Max(0.1f, _overSpeedRange));
            // Сверхскорость без контакта с дорогой не создаёт прежнюю нагрузку.
            float contactFactor = 1f - _airborneBlend;
            float extraPitchMultiplier = (1f +
                _boostBlend * _boostPitchBonus * contactFactor +
                overSpeed01 * _overSpeedPitchBonus * contactFactor) *
                Mathf.Lerp(1f, _airbornePitchMultiplier, _airborneBlend);
            float driveVolumeMultiplier = 1f + _boostBlend * _boostVolumeBonus * contactFactor;

            float mix = Mathf.SmoothStep(0f, 1f, _blend);
            _idleSource.volume = _idleVolume * (1f - mix) * _master;
            _driveSource.volume = Mathf.Min(1f, _driveVolume * mix * _master * driveVolumeMultiplier);

            _idleSource.pitch = Mathf.Lerp(_idlePitchMin,
                Mathf.Max(_idlePitchMin, _idlePitchMax), _tone) * extraPitchMultiplier;
            _driveSource.pitch = Mathf.Lerp(_drivePitchMin,
                Mathf.Max(_drivePitchMin, _drivePitchMax), _tone) * extraPitchMultiplier;
        }
    }
}
