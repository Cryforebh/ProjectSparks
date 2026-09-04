using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class ProgressResetView : MonoBehaviour
    {
        [Tooltip("Кнопка открытия подтверждения сброса.")]
        [SerializeField] private Button _openButton;
        [Tooltip("Панель подтверждения сброса прогресса.")]
        [SerializeField] private GameObject _confirmationPanel;
        [Tooltip("Кнопка подтверждения сброса прогресса.")]
        [SerializeField] private Button _confirmButton;
        [Tooltip("Кнопка отмены сброса прогресса.")]
        [SerializeField] private Button _cancelButton;

        private IProgressService _progressService;
        private bool _isResetting;

        [Inject]
        public void Construct(IProgressService progressService)
        {
            _progressService = progressService;
        }

        private void Start()
        {
            if (_openButton == null
                || _confirmationPanel == null
                || _confirmButton == null
                || _cancelButton == null)
            {
                Debug.LogError("В ProgressResetView назначены не все ссылки.", this);
                enabled = false;
                return;
            }

            _openButton.onClick.AddListener(HandleOpenClicked);
            _confirmButton.onClick.AddListener(HandleConfirmClicked);
            _cancelButton.onClick.AddListener(HandleCancelClicked);
            SetConfirmationVisible(false);
        }

        private void OnDisable()
        {
            if (!_isResetting)
            {
                SetConfirmationVisible(false);
            }
        }

        private void OnDestroy()
        {
            if (_openButton != null)
            {
                _openButton.onClick.RemoveListener(HandleOpenClicked);
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(HandleCancelClicked);
            }
        }

        private void HandleOpenClicked()
        {
            SetConfirmationVisible(true);
        }

        private void HandleCancelClicked()
        {
            SetConfirmationVisible(false);
        }

        private void HandleConfirmClicked()
        {
            if (_isResetting)
            {
                return;
            }

            ResetProgressAsync().Forget();
        }

        private async UniTask ResetProgressAsync()
        {
            _isResetting = true;
            SetButtonsInteractable(false);

            try
            {
                await _progressService.ResetAsync(this.GetCancellationTokenOnDestroy());
                SetConfirmationVisible(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                _isResetting = false;
                SetButtonsInteractable(true);
            }
        }

        private void SetButtonsInteractable(bool isInteractable)
        {
            if (_confirmButton != null)
            {
                _confirmButton.interactable = isInteractable;
            }

            if (_cancelButton != null)
            {
                _cancelButton.interactable = isInteractable;
            }
        }

        private void SetConfirmationVisible(bool isVisible)
        {
            if (_confirmationPanel != null
                && _confirmationPanel.activeSelf != isVisible)
            {
                _confirmationPanel.SetActive(isVisible);
            }
        }
    }
}
