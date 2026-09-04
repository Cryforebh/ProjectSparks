using ProjectSparks.Core;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuView : MonoBehaviour
    {
        [Tooltip("Панель меню, которая отображается во время паузы.")]
        [SerializeField] private GameObject _panel;
        [Tooltip("Кнопка продолжения игры.")]
        [SerializeField] private Button _resumeButton;

        private IPauseService _pauseService;

        [Inject]
        private void Construct(IPauseService pauseService)
        {
            _pauseService = pauseService;
        }

        private void Start()
        {
            _pauseService.PauseChanged += HandlePauseChanged;
            _resumeButton.onClick.AddListener(HandleResumeClicked);
            SetVisible(_pauseService.IsPaused);
        }

        private void OnDestroy()
        {
            if (_pauseService != null)
            {
                _pauseService.PauseChanged -= HandlePauseChanged;
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(HandleResumeClicked);
            }
        }

        private void HandlePauseChanged(bool isPaused)
        {
            SetVisible(isPaused);
        }

        private void HandleResumeClicked()
        {
            _pauseService.SetPaused(false);
        }

        private void SetVisible(bool isVisible)
        {
            if (_panel != null)
            {
                _panel.SetActive(isVisible);
            }
        }
    }
}
