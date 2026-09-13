using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Collectibles;
using ProjectSparks.Core;
using ProjectSparks.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class TripResultsView : MonoBehaviour
    {
        [Tooltip("Окно временных итогов поездки.")]
        [SerializeField] private GameObject _panel;
        [Tooltip("Кнопка открытия итогов после последней доставки.")]
        [SerializeField] private Button _openButton;
        [Tooltip("Показывать кнопку возврата поверх игры. Для ПК можно выключить: возврат останется в меню паузы.")]
        [SerializeField] private bool _showGameplayReturnButton = true;
        [Tooltip("Кнопка возврата из меню паузы. До завершения доставок открывает предупреждение о потере наград.")]
        [SerializeField] private Button _pauseOpenButton;
        [Tooltip("Кнопка закрытия итогов и продолжения поездки.")]
        [SerializeField] private Button _continueButton;
        [Tooltip("Текст доставок и временных наград поездки.")]
        [SerializeField] private TMP_Text _summaryText;
        [Tooltip("Кнопка сохранения результата и возврата в убежище.")]
        [SerializeField] private Button _confirmButton;
        [Tooltip("Текст состояния сохранения или ошибки возврата.")]
        [SerializeField] private TMP_Text _statusText;
        [Tooltip("Имя сцены убежища.")]
        [SerializeField] private string _shelterSceneName = "Shelter";

        [Tooltip("Общее окно подтверждения выхода без сохранения наград.")]
        [SerializeField] private TripExitView _tripExitView;

        private ITripSessionService _tripSession;
        private IPauseService _pauseService;
        private TripCompletionService _completionService;
        private bool _isBusy;
        private bool _isSubscribed;
        private bool _isOpen;

        [Zenject.Inject]
        private void Construct(ITripSessionService tripSession, IPauseService pauseService,
            TripCompletionService completionService)
        {
            _tripSession = tripSession;
            _pauseService = pauseService;
            _completionService = completionService;
        }

        private void Start()
        {
            if (_panel == null || _openButton == null || _continueButton == null || _summaryText == null ||
                _confirmButton == null || _statusText == null)
            {
                Debug.LogError("В TripResultsView назначены не все элементы интерфейса.", this);
                enabled = false;
                return;
            }

            Subscribe();
        }

        private void OnEnable()
        {
            if (_tripSession != null && _panel != null && _openButton != null &&
                _continueButton != null && _summaryText != null && _confirmButton != null && _statusText != null)
            {
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            _tripSession.Changed += Refresh;
            _pauseService.PauseChanged += HandlePauseChanged;
            _openButton.onClick.AddListener(Open);
            if (_pauseOpenButton != null)
                _pauseOpenButton.onClick.AddListener(RequestReturn);
            _continueButton.onClick.AddListener(Close);
            _confirmButton.onClick.AddListener(Confirm);
            _isSubscribed = true;
            _panel.SetActive(false);
            Refresh();
        }

        private void OnDisable()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _tripSession.Changed -= Refresh;
            _pauseService.PauseChanged -= HandlePauseChanged;
            _openButton.onClick.RemoveListener(Open);
            if (_pauseOpenButton != null)
            {
                _pauseOpenButton.onClick.RemoveListener(RequestReturn);
                _pauseOpenButton.gameObject.SetActive(false);
            }
            _continueButton.onClick.RemoveListener(Close);
            _confirmButton.onClick.RemoveListener(Confirm);
            _isSubscribed = false;
            ClosePanel();
            _openButton.gameObject.SetActive(false);
        }

        public void RequestReturn()
        {
            if (!isActiveAndEnabled || _isOpen || _isBusy ||
                (_pauseService.IsPaused && !_pauseService.IsPauseMenuVisible))
                return;
            if (_tripSession.State == TripSessionState.Succeeded || _tripSession.State == TripSessionState.Confirmed)
                Open();
            else if (_tripExitView != null)
                _tripExitView.RequestExitTo(_shelterSceneName);
        }

        private void Open()
        {
            if (_isOpen || (_pauseService.IsPaused && !_pauseService.IsPauseMenuVisible) ||
                (_tripSession.State != TripSessionState.Succeeded && _tripSession.State != TripSessionState.Confirmed))
            {
                return;
            }

            _isOpen = true;
            _statusText.text = _tripSession.State == TripSessionState.Confirmed
                ? "Награды сохранены. Можно повторить переход в убежище."
                : "Подтвердите возврат, чтобы сохранить награды поездки.";
            _panel.SetActive(true);
            _pauseService.SetPaused(true, false);
            Refresh();
            _continueButton.Select();
        }

        private void Close()
        {
            if (_isBusy || _tripSession.State == TripSessionState.Confirmed)
            {
                return;
            }

            ClosePanel();
        }

        private void ClosePanel()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            if (_pauseService.IsPaused && !_pauseService.IsPauseMenuVisible)
            {
                _pauseService.SetPaused(false);
            }

            if (_isSubscribed)
            {
                Refresh();
            }
        }

        private void HandlePauseChanged(bool isPaused)
        {
            if (_isOpen && (_isBusy || _tripSession.State == TripSessionState.Confirmed))
            {
                if (!isPaused || _pauseService.IsPauseMenuVisible)
                {
                    _pauseService.SetPaused(true, false);
                }

                return;
            }

            if (_isOpen && (!isPaused || _pauseService.IsPauseMenuVisible))
            {
                Close();
            }

            Refresh();
        }

        private void Refresh()
        {
            bool succeeded = _tripSession.State == TripSessionState.Succeeded ||
                _tripSession.State == TripSessionState.Confirmed;
            if (_isOpen && !succeeded)
            {
                Close();
            }

            RefreshReturnButtons(succeeded);
            if (!_isOpen)
            {
                return;
            }

            _continueButton.interactable = !_isBusy && _tripSession.State != TripSessionState.Confirmed;
            _confirmButton.interactable = !_isBusy;

            _summaryText.text =
                $"Доставки: {_tripSession.CompletedDeliveryCount} / {_tripSession.RequiredDeliveryCount}\n" +
                $"Монеты: {_tripSession.Coins}\n" +
                $"Капсулы: {_tripSession.CapsuleCount}\n\n" +
                $"Фрукты плавания: {_tripSession.GetFruitAmount(FruitType.Swim)}\n" +
                $"Фрукты полета: {_tripSession.GetFruitAmount(FruitType.Fly)}\n" +
                $"Фрукты бега: {_tripSession.GetFruitAmount(FruitType.Run)}\n" +
                $"Фрукты силы: {_tripSession.GetFruitAmount(FruitType.Power)}\n" +
                $"Фрукты выносливости: {_tripSession.GetFruitAmount(FruitType.Stamina)}";
        }

        private void LateUpdate()
        {
            if (_isSubscribed)
                RefreshReturnButtons(_tripSession.State == TripSessionState.Succeeded ||
                    _tripSession.State == TripSessionState.Confirmed);
        }

        private void RefreshReturnButtons(bool succeeded)
        {
            _openButton.gameObject.SetActive(_showGameplayReturnButton && succeeded && !_isOpen && !_pauseService.IsPaused);
            if (_pauseOpenButton != null)
                _pauseOpenButton.gameObject.SetActive(!_isOpen);
        }

        private void Confirm()
        {
            if (!_isOpen || _isBusy)
            {
                return;
            }

            ConfirmAsync().Forget();
        }

        private async UniTask ConfirmAsync()
        {
            _isBusy = true;
            _statusText.text = "Сохранение результата и возврат в убежище...";
            Refresh();
            try
            {
                await _completionService.CompleteAsync(_shelterSceneName, this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                if (this != null && _statusText != null)
                {
                    _statusText.text = "Возврат прерван. Можно повторить попытку.";
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                if (this != null && _statusText != null)
                {
                    _statusText.text = _tripSession.State == TripSessionState.Confirmed
                        ? "Награды сохранены. Не удалось открыть убежище. Повторите возврат."
                        : "Не удалось сохранить результат. Награды поездки сохранены в памяти. Повторите попытку.";
                }
            }
            finally
            {
                _isBusy = false;
                if (this != null && _isSubscribed)
                {
                    Refresh();
                }
            }
        }
    }
}
