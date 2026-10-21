using System;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Missions
{
    /// <summary>
    /// Звуковая обратная связь для получения и обычной сдачи посылок.
    /// Последняя сдача намеренно остаётся без обычного звука:
    /// её озвучивает отдельный TripCompletionAudio.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MissionDeliveryAudio : MonoBehaviour
    {
        [Header("Звуки доставок")]
        [Tooltip("Звук получения груза на складе.")]
        [SerializeField] private AudioClip _pickupClip;
        [Tooltip("Звук сдачи груза, кроме последней доставки.")]
        [SerializeField] private AudioClip _deliveryClip;

        [Header("Громкость")]
        [SerializeField, Range(0f, 1f)] private float _pickupVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _deliveryVolume = 0.65f;

        private IMissionService _missions;
        private ITripSessionService _trip;
        private AudioSource _source;
        private MissionPhase _previousPhase;
        private string _sessionId;
        private int _previousCompletedCount;
        private bool _subscribed;

        [Inject]
        private void Construct(IMissionService missions, ITripSessionService trip)
        {
            _missions = missions;
            _trip = trip;
            Subscribe();
        }

        private void Awake() => _source = GetComponent<AudioSource>();

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || !isActiveAndEnabled || _missions == null || _trip == null)
                return;

            // Начинаем наблюдать с текущего состояния, не воспроизводя старые события.
            _previousPhase = _missions.Phase;
            _sessionId = _trip.SessionId;
            _previousCompletedCount = _trip.CompletedDeliveryCount;

            _missions.StateChanged += OnMissionChanged;
            _trip.Changed += OnTripChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            _missions.StateChanged -= OnMissionChanged;
            _trip.Changed -= OnTripChanged;
            _subscribed = false;
        }

        private void OnMissionChanged()
        {
            MissionPhase current = _missions.Phase;
            if (_previousPhase == MissionPhase.AwaitingPickup &&
                current == MissionPhase.Delivering &&
                _trip.State == TripSessionState.InProgress)
            {
                Play(_pickupClip, _pickupVolume);
            }

            _previousPhase = current;
        }

        private void OnTripChanged()
        {
            // Новый выезд: счётчик может обнулиться, это не сдача посылки.
            if (!string.Equals(_sessionId, _trip.SessionId, StringComparison.Ordinal))
            {
                _sessionId = _trip.SessionId;
                _previousCompletedCount = _trip.CompletedDeliveryCount;
                return;
            }

            int count = _trip.CompletedDeliveryCount;
            if (count > _previousCompletedCount &&
                _trip.State == TripSessionState.InProgress &&
                count < _trip.RequiredDeliveryCount)
            {
                Play(_deliveryClip, _deliveryVolume);
            }

            _previousCompletedCount = count;
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && _source != null)
                _source.PlayOneShot(clip, volume);
        }

        private void Reset()
        {
            AudioSource source = GetComponent<AudioSource>();
            if (source == null)
                return;

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f; // Отклик интерфейса, не звук из мира.
        }
    }
}
