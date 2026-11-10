using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class TerrariumGuideView : MonoBehaviour
    {
        [Serializable]
        private sealed class Step
        {
            [Tooltip("Заголовок шага.")]
            [SerializeField] private string _title;
            [Tooltip("Короткое пояснение к снимку.")]
            [SerializeField, TextArea] private string _description;
            [Tooltip("Снимок из игры для этого шага.")]
            [SerializeField] private Sprite _image;

            public string Title => _title;
            public string Description => _description;
            public Sprite Image => _image;
        }

        [Tooltip("Вводная страница без снимка.")]
        [SerializeField] private GameObject _intro;
        [Tooltip("Область со снимком и подписью.")]
        [SerializeField] private GameObject _stepPanel;
        [Tooltip("Текущий заголовок страницы.")]
        [SerializeField] private TMP_Text _title;
        [Tooltip("Номер шага или сообщение о сохранении.")]
        [SerializeField] private TMP_Text _status;
        [Tooltip("Текст под снимком.")]
        [SerializeField] private TMP_Text _description;
        [Tooltip("Изображение текущего шага.")]
        [SerializeField] private Image _image;
        [Tooltip("Кнопка предыдущей страницы.")]
        [SerializeField] private Button _backButton;
        [Tooltip("Кнопка следующей страницы или завершения.")]
        [SerializeField] private Button _nextButton;
        [Tooltip("Подпись кнопки следующей страницы.")]
        [SerializeField] private TMP_Text _nextLabel;
        [Tooltip("Кнопка пропуска инструктажа.")]
        [SerializeField] private Button _skipButton;
        [Tooltip("Шаги инструктажа в порядке показа.")]
        [SerializeField] private Step[] _steps;

        private int _page;
        private bool _isSaving;

        public event Action Completed;

        private void Awake()
        {
            _backButton.onClick.AddListener(GoBack);
            _nextButton.onClick.AddListener(GoNext);
            _skipButton.onClick.AddListener(Finish);
        }

        private void OnDestroy()
        {
            _backButton.onClick.RemoveListener(GoBack);
            _nextButton.onClick.RemoveListener(GoNext);
            _skipButton.onClick.RemoveListener(Finish);
        }

        public void Show()
        {
            ShowPage(0);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetSaving()
        {
            _isSaving = true;
            SetButtonsEnabled(false);
            _status.text = "Сохраняем прогресс...";
        }

        public void ShowSaveError()
        {
            _isSaving = false;
            SetButtonsEnabled(true);
            _status.text = "Не удалось сохранить. Попробуйте еще раз.";
            _nextButton.Select();
        }

        private void GoBack()
        {
            if (!_isSaving && _page > 0)
            {
                ShowPage(_page - 1);
            }
        }

        private void GoNext()
        {
            if (_isSaving)
            {
                return;
            }

            if (_page == _steps.Length)
            {
                Finish();
                return;
            }

            ShowPage(_page + 1);
        }

        private void Finish()
        {
            if (!_isSaving)
            {
                Completed?.Invoke();
            }
        }

        private void ShowPage(int page)
        {
            _page = page;
            bool isIntro = page == 0;
            _intro.SetActive(isIntro);
            _stepPanel.SetActive(!isIntro);
            _backButton.gameObject.SetActive(!isIntro);

            _status.text = isIntro ? "ПЕРВОЕ ПОСЕЩЕНИЕ" : $"ШАГ {page} ИЗ {_steps.Length}";
            _title.text = "Добро пожаловать в Террариум";
            if (!isIntro)
            {
                Step step = _steps[page - 1];
                _title.text = step.Title;
                _description.text = step.Description;
                _image.sprite = step.Image;
            }

            _nextLabel.text = page == _steps.Length ? "В Террариум" : isIntro ? "Начать" : "Далее";
            Navigation navigation = _nextButton.navigation;
            navigation.selectOnLeft = isIntro ? _skipButton : _backButton;
            _nextButton.navigation = navigation;
            _nextButton.Select();
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _backButton.interactable = enabled;
            _nextButton.interactable = enabled;
            _skipButton.interactable = enabled;
        }
    }
}
