using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using ProjectSparks.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class TripExitView : MonoBehaviour
    {
        [Tooltip("Окно предупреждения о потере результата поездки.")]
        [SerializeField] private GameObject _panel;
        [Tooltip("Текст предупреждения или ошибки выхода.")]
        [SerializeField] private TMP_Text _messageText;
        [Tooltip("Кнопка подтверждения выхода без наград.")]
        [SerializeField] private Button _confirmButton;
        [Tooltip("Кнопка отмены выхода и продолжения игры.")]
        [SerializeField] private Button _cancelButton;
        [Tooltip("Имя сцены главного меню.")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        private TripExitService _exitService;
        private ITripSessionService _session;
        private IPauseService _pauseService;
        private bool _isOpen;
        private bool _isBusy;
        private CursorLockMode _previousCursorLock;
        private bool _previousCursorVisible;

        [Inject]
        private void Construct(TripExitService exitService, ITripSessionService session, IPauseService pauseService)
        {
            _exitService = exitService;
            _session = session;
            _pauseService = pauseService;
        }

        private void Start()
        {
            if (_panel == null || _messageText == null || _confirmButton == null || _cancelButton == null)
            {
                Debug.LogError("В TripExitView назначены не все элементы интерфейса.", this);
                enabled = false;
                return;
            }

            _confirmButton.onClick.AddListener(Confirm);
            _cancelButton.onClick.AddListener(Cancel);
            _pauseService.PauseChanged += HandlePauseChanged;
            _panel.SetActive(false);
        }

        private void OnDisable()
        {
            Close();
        }

        private void OnDestroy()
        {
            if (_pauseService != null)
            {
                _pauseService.PauseChanged -= HandlePauseChanged;
            }
            if (_confirmButton != null) _confirmButton.onClick.RemoveListener(Confirm);
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(Cancel);
        }

        public void RequestExit()
        {
            if (!isActiveAndEnabled || _isOpen || _isBusy)
            {
                return;
            }

            _previousCursorLock = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _isOpen = true;
            _panel.SetActive(true);
            _pauseService.SetPaused(true, false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _messageText.text = _session.State == TripSessionState.Succeeded
                ? "Результат поездки не сохранен. Собранные ресурсы и награды будут потеряны. Выйти?"
                : "Доставка не завершена. Собранные ресурсы и награды будут потеряны. Выйти?";
            SetButtonsEnabled(true);
            if (_exitService.RequiresConfirmation)
            {
                _cancelButton.Select();
            }
            else
            {
                Confirm();
            }
        }

        private void Cancel()
        {
            if (!_isBusy)
            {
                Close();
            }
        }

        private void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            if (_panel != null) _panel.SetActive(false);
            Cursor.lockState = _previousCursorLock;
            Cursor.visible = _previousCursorVisible;
            if (_pauseService.IsPaused && !_pauseService.IsPauseMenuVisible)
            {
                _pauseService.SetPaused(false);
            }
        }

        private void HandlePauseChanged(bool isPaused)
        {
            if (!_isOpen || (isPaused && !_pauseService.IsPauseMenuVisible))
            {
                return;
            }

            if (_isBusy)
            {
                _pauseService.SetPaused(true, false);
            }
            else
            {
                Close();
            }
        }

        private void Confirm()
        {
            if (_isOpen && !_isBusy)
            {
                ExitAsync().Forget();
            }
        }

        private async UniTask ExitAsync()
        {
            _isBusy = true;
            SetButtonsEnabled(false);
            _messageText.text = "Возврат в главное меню...";
            try
            {
                await _exitService.ExitAsync(_mainMenuSceneName, this.GetCancellationTokenOnDestroy());
            }
            catch (Exception exception)
            {
                if (this != null && _messageText != null)
                {
                    _messageText.text = "Не удалось открыть главное меню. Результат поездки не удален. Повторите выход или продолжите игру.";
                    Debug.LogException(exception, this);
                }
            }
            finally
            {
                _isBusy = false;
                if (this != null)
                {
                    SetButtonsEnabled(true);
                }
            }
        }

        private void SetButtonsEnabled(bool isEnabled)
        {
            _confirmButton.interactable = isEnabled;
            _cancelButton.interactable = isEnabled;
        }
    }
}
