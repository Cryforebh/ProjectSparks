using UnityEngine;

namespace ProjectSparks.Missions
{
    /// <summary>Проигрывает только звук завершения всех доставок.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class TripCompletionAudio : MonoBehaviour
    {
        [SerializeField] private TripCompletionNotifier _notifier;
        [SerializeField] private AudioClip _clip;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.65f;

        private AudioSource _source;

        private void Awake() => _source = GetComponent<AudioSource>();

        private void OnEnable()
        {
            if (_notifier != null)
                _notifier.Completed += Play;
        }

        private void OnDisable()
        {
            if (_notifier != null)
                _notifier.Completed -= Play;
        }

        private void Play()
        {
            if (_source != null && _clip != null)
                _source.PlayOneShot(_clip, _volume);
        }

        private void Reset()
        {
            AudioSource source = GetComponent<AudioSource>();
            if (source == null)
                return;

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }
    }
}
