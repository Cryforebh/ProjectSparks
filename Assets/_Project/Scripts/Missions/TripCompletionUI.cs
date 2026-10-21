using System.Collections;
using UnityEngine;

namespace ProjectSparks.Missions
{
    /// <summary>
    /// Только отображает временное поздравление и постоянную цель возвращения.
    /// Тексты задаются в самих элементах UI (например, TextMeshPro).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TripCompletionUI : MonoBehaviour
    {
        [Header("Событие")]
        [SerializeField] private TripCompletionNotifier _notifier;

        [Header("Панели Canvas")]
        [Tooltip("CanvasGroup большого поздравления.")]
        [SerializeField] private CanvasGroup _banner;
        [Tooltip("CanvasGroup постоянного напоминания о возвращении.")]
        [SerializeField] private CanvasGroup _returnReminder;

        [Header("Время (секунды)")]
        [SerializeField, Min(0f)] private float _fadeIn = 0.22f;
        [SerializeField, Min(0f)] private float _hold = 3.8f;
        [SerializeField, Min(0f)] private float _fadeOut = 0.45f;

        private Coroutine _animation;

        private void OnEnable()
        {
            if (_notifier != null)
            {
                _notifier.Completed += ShowCompletion;
                _notifier.Cleared += HideAll;
            }

            SetVisible(_banner, false);
            // Если UI включён повторно после поздравления, оставляем только цель.
            SetVisible(_returnReminder, _notifier != null && _notifier.IsComplete);
        }

        private void OnDisable()
        {
            if (_notifier != null)
            {
                _notifier.Completed -= ShowCompletion;
                _notifier.Cleared -= HideAll;
            }

            StopAnimation();
            SetVisible(_banner, false);
            SetVisible(_returnReminder, false);
        }

        private void ShowCompletion()
        {
            StopAnimation();
            SetVisible(_banner, false);
            SetVisible(_returnReminder, false);

            if (_banner == null)
            {
                SetVisible(_returnReminder, true);
                return;
            }

            _animation = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            yield return Fade(_banner, 1f, _fadeIn);
            if (_hold > 0f)
                yield return new WaitForSecondsRealtime(_hold);
            yield return Fade(_banner, 0f, _fadeOut);

            SetVisible(_returnReminder, true);
            _animation = null;
        }

        private void HideAll()
        {
            StopAnimation();
            SetVisible(_banner, false);
            SetVisible(_returnReminder, false);
        }

        private void StopAnimation()
        {
            if (_animation == null)
                return;

            StopCoroutine(_animation);
            _animation = null;
        }

        private static IEnumerator Fade(CanvasGroup group, float to, float seconds)
        {
            float from = group.alpha;
            if (seconds <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float time = 0f;
            while (time < seconds)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / seconds);
                t = t * t * (3f - 2f * t);
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            group.alpha = to;
        }

        private static void SetVisible(CanvasGroup group, bool visible)
        {
            if (group == null)
                return;

            group.alpha = visible ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}
