using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using ProjectSparks.Levels;
using ProjectSparks.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelSelectionView : MonoBehaviour
    {
        [Tooltip("Панель выбора. Корневой объект компонента остается активным.")]
        [SerializeField] private GameObject _panel;
        [Tooltip("Шаблон карточки внутри прокручиваемого списка.")]
        [SerializeField] private LevelCardView _cardTemplate;
        [Tooltip("Контейнер карточек локаций.")]
        [SerializeField] private RectTransform _content;
        [Tooltip("Текст подсказки или ошибки отправления.")]
        [SerializeField] private TMP_Text _statusText;
        [Tooltip("Кнопка закрытия выбора.")]
        [SerializeField] private Button _closeButton;
        [Tooltip("Кнопка перехода в убежище для выбора Искры.")]
        [SerializeField] private Button _shelterButton;
        [Tooltip("Имя сцены убежища.")]
        [SerializeField] private string _shelterSceneName = "Shelter";

        private LevelSelectionService _selection;
        private ISceneTransitionService _transition;
        private IPauseService _pause;
        private IPlayerControlService _control;
        private readonly List<LevelCardView> _cards = new List<LevelCardView>();
        private bool _isOpen;
        private bool _isBusy;
        private bool _isReady;
        private bool _wasControlEnabled;
        private CursorLockMode _previousCursorLock;
        private bool _previousCursorVisible;

        [Inject]
        private void Construct(LevelSelectionService selection, ISceneTransitionService transition,
            [InjectOptional] IPauseService pause = null, [InjectOptional] IPlayerControlService control = null)
        {
            _selection = selection;
            _transition = transition;
            _pause = pause;
            _control = control;
        }

        private void Start()
        {
            _panel.SetActive(false);
            _cardTemplate.gameObject.SetActive(false);
            for (int i = 0; i < _selection.Catalog.Count; i++)
            {
                LevelCatalog.Entry entry = _selection.Catalog.GetEntry(i);
                if (entry?.Level == null)
                    continue;
                LevelCardView card = Instantiate(_cardTemplate, _content);
                card.Initialize(entry.Level, _selection.Catalog.CanLaunch(entry.Level), Launch);
                card.gameObject.SetActive(true);
                var rect = (RectTransform)card.transform;
                rect.anchoredPosition = new Vector2(0f, -_cards.Count * 190f);
                _cards.Add(card);
            }
            _content.sizeDelta = new Vector2(0f, _cards.Count * 190f);
            _selection.OpenRequested += Open;
            _closeButton.onClick.AddListener(Close);
            _shelterButton.onClick.AddListener(GoToShelter);
            if (_pause != null)
                _pause.PauseChanged += HandlePauseChanged;
        }

        private void Update()
        {
            if (_isOpen && _pause == null && Keyboard.current != null &&
                (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame))
                Close();
        }

        private void Open()
        {
            if (_isOpen || (_pause?.IsPaused ?? false) || (_control != null && !_control.IsControlEnabled))
                return;
            _previousCursorLock = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _wasControlEnabled = _control?.IsControlEnabled ?? false;
            _isOpen = true;
            _panel.SetActive(true);
            _control?.SetControlEnabled(false);
            _pause?.SetPaused(true, false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PrepareAsync().Forget();
        }

        private async UniTask PrepareAsync()
        {
            _isBusy = true;
            _isReady = false;
            _statusText.text = "Загрузка прогресса...";
            Refresh();
            try
            {
                await _selection.PrepareAsync(this.GetCancellationTokenOnDestroy());
                _isReady = true;
                _statusText.text = _selection.HasCompanion
                    ? "Выберите локацию для следующей поездки."
                    : "Сначала выберите Искру в убежище, затем отправляйтесь на доставку.";
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                if (this != null)
                    _statusText.text = "Не удалось загрузить прогресс. Закройте окно и попробуйте снова.";
            }
            finally
            {
                _isBusy = false;
                if (this != null)
                {
                    Refresh();
                    _closeButton.Select();
                }
            }
        }

        private void Refresh()
        {
            foreach (LevelCardView card in _cards)
                card.SetCanLaunch(_isReady && !_isBusy && _selection.HasCompanion);
            _closeButton.interactable = !_isBusy;
            _shelterButton.gameObject.SetActive(SceneManager.GetActiveScene().name != _shelterSceneName);
            _shelterButton.interactable = !_isBusy;
        }

        private void Close()
        {
            if (!_isBusy)
                ClosePanel();
        }

        private void ClosePanel()
        {
            if (!_isOpen)
                return;
            _isOpen = false;
            if (_panel != null)
                _panel.SetActive(false);
            _control?.SetControlEnabled(_wasControlEnabled);
            if (_pause != null && _pause.IsPaused && !_pause.IsPauseMenuVisible)
                _pause.SetPaused(false);
            Cursor.lockState = _previousCursorLock;
            Cursor.visible = _previousCursorVisible;
        }

        private void HandlePauseChanged(bool isPaused)
        {
            if (!_isOpen || (isPaused && !_pause.IsPauseMenuVisible))
                return;
            if (_isBusy)
                _pause.SetPaused(true, false);
            else
                ClosePanel();
        }

        private void Launch(LevelDefinition level)
        {
            if (_isOpen && !_isBusy && _isReady)
                NavigateAsync(level).Forget();
        }

        private void GoToShelter()
        {
            if (_isOpen && !_isBusy)
                NavigateAsync(null).Forget();
        }

        private async UniTask NavigateAsync(LevelDefinition level)
        {
            _isBusy = true;
            _statusText.text = "Переход...";
            Refresh();
            try
            {
                if (level != null)
                    await _selection.LaunchAsync(level, this.GetCancellationTokenOnDestroy());
                else
                    await _transition.TransitionAsync(_shelterSceneName, string.Empty, this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                if (this != null)
                    _statusText.text = "Не удалось отправиться. Проверьте выбранную Искру и повторите попытку.";
            }
            finally
            {
                _isBusy = false;
                if (this != null)
                    Refresh();
            }
        }

        private void OnDestroy()
        {
            if (_selection != null)
                _selection.OpenRequested -= Open;
            if (_pause != null)
                _pause.PauseChanged -= HandlePauseChanged;
            _closeButton.onClick.RemoveListener(Close);
            _shelterButton.onClick.RemoveListener(GoToShelter);
            ClosePanel();
        }
    }
}
