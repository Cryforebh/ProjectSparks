using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class SceneFadeView : MonoBehaviour
    {
        [Tooltip("Группа Canvas, которая затемняет экран.")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [Tooltip("Продолжительность затемнения и проявления в секундах.")]
        [SerializeField, Min(0f)] private float _fadeDuration = 0.35f;

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                Debug.LogError("В SceneFadeView не назначен Canvas Group.", this);
                enabled = false;
                return;
            }

            ShowImmediately(false);
        }

        public async UniTask FadeOutAsync(CancellationToken cancellationToken)
        {
            _canvasGroup.blocksRaycasts = true;
            await FadeToAsync(1f, cancellationToken);
        }

        public async UniTask FadeInAsync(CancellationToken cancellationToken)
        {
            await FadeToAsync(0f, cancellationToken);
            _canvasGroup.blocksRaycasts = false;
        }

        public void ShowImmediately(bool isVisible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = isVisible;
        }

        private async UniTask FadeToAsync(
            float targetAlpha,
            CancellationToken cancellationToken)
        {
            float startAlpha = _canvasGroup.alpha;
            if (_fadeDuration <= 0f)
            {
                _canvasGroup.alpha = targetAlpha;
                return;
            }

            float elapsedTime = 0f;
            while (elapsedTime < _fadeDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsedTime += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    Mathf.Clamp01(elapsedTime / _fadeDuration));
                await UniTask.Yield(cancellationToken);
            }

            _canvasGroup.alpha = targetAlpha;
        }
    }
}
