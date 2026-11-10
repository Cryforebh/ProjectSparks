using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class SceneFadeView : MonoBehaviour
    {
        [Serializable]
        private sealed class LoadingTip
        {
            [Tooltip("Снимок игры для подсказки.")]
            [SerializeField] private Sprite _image;
            [Tooltip("Короткая подсказка под снимком.")]
            [SerializeField, TextArea] private string _text;

            public Sprite Image => _image;
            public string Text => _text;
        }

        [Tooltip("Группа Canvas, которая плавно показывает и скрывает экран загрузки.")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [Tooltip("Карточка подсказки, которая гаснет до появления сцены.")]
        [SerializeField] private CanvasGroup _loadingCard;
        [Tooltip("Продолжительность затемнения и проявления в секундах.")]
        [SerializeField, Min(0f)] private float _fadeDuration = 0.35f;
        [Tooltip("Иллюстрация текущей подсказки.")]
        [SerializeField] private Image _tipImage;
        [Tooltip("Текст текущей подсказки.")]
        [SerializeField] private TMP_Text _tipText;
        [Tooltip("Заполнение полосы загрузки сцены.")]
        [SerializeField] private Image _progressFill;
        [Tooltip("Подсказки, из которых выбирается одна при каждом переходе.")]
        [SerializeField] private LoadingTip[] _tips = Array.Empty<LoadingTip>();

        private int _lastTipIndex = -1;

        private void Awake()
        {
            if (_canvasGroup == null || _loadingCard == null)
            {
                Debug.LogError("В SceneFadeView назначьте группы экрана и карточки загрузки.", this);
                enabled = false;
                return;
            }

            ShowImmediately(false);
        }

        public void PrepareLoadingScreen()
        {
            _loadingCard.alpha = 1f;
            SetProgress(0f);
            if (_tips.Length == 0)
            {
                return;
            }

            // При нескольких подсказках исключаем предыдущую из случайного выбора.
            int index = _lastTipIndex < 0 || _tips.Length == 1
                ? Random.Range(0, _tips.Length)
                : (_lastTipIndex + Random.Range(1, _tips.Length)) % _tips.Length;

            _lastTipIndex = index;
            LoadingTip tip = _tips[index];
            _tipImage.sprite = tip.Image;
            _tipText.text = tip.Text;
        }

        public void SetProgress(float progress)
        {
            _progressFill.fillAmount = progress;
        }

        public UniTask ShowLoadingScreenAsync(CancellationToken cancellationToken)
        {
            _canvasGroup.blocksRaycasts = true;
            return FadeToAsync(_canvasGroup, 1f, cancellationToken);
        }

        public async UniTask RevealSceneAsync(CancellationToken cancellationToken)
        {
            await FadeToAsync(_loadingCard, 0f, cancellationToken);
            await FadeToAsync(_canvasGroup, 0f, cancellationToken);
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
            CanvasGroup group, float targetAlpha, CancellationToken cancellationToken)
        {
            float startAlpha = group.alpha;
            if (_fadeDuration <= 0f)
            {
                group.alpha = targetAlpha;
                return;
            }

            float elapsedTime = 0f;
            while (elapsedTime < _fadeDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsedTime += Time.unscaledDeltaTime;
                group.alpha = Mathf.SmoothStep(
                    startAlpha,
                    targetAlpha,
                    Mathf.Clamp01(elapsedTime / _fadeDuration));
                await UniTask.Yield(cancellationToken);
            }

            group.alpha = targetAlpha;
        }
    }
}
