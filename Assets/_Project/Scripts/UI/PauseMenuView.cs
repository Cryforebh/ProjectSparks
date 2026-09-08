using System;
using Cysharp.Threading.Tasks;
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
        [Tooltip("Кнопка возврата в главное меню.")]
        [SerializeField] private Button _mainMenuButton;
        [Tooltip("Имя сцены главного меню.")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        [Tooltip("Подтверждение выхода из поездки. В сценах без поездки можно не назначать.")]
        [SerializeField] private TripExitView _tripExitView;

        private IPauseService _pauseService;
        private ISceneLoader _sceneLoader;

        [Inject]
        private void Construct(
            IPauseService pauseService,
            ISceneLoader sceneLoader)
        {
            _pauseService = pauseService;
            _sceneLoader = sceneLoader;
        }

        private void Start()
        {
            _pauseService.PauseChanged += HandlePauseChanged;
            _resumeButton.onClick.AddListener(HandleResumeClicked);
            _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
            SetVisible(_pauseService.IsPauseMenuVisible);
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

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
            }
        }

        private void HandlePauseChanged(bool isPaused)
        {
            SetVisible(_pauseService.IsPauseMenuVisible);
        }

        private void HandleResumeClicked()
        {
            _pauseService.SetPaused(false);
        }

        private void HandleMainMenuClicked()
        {
            if (_tripExitView != null)
            {
                _tripExitView.RequestExit();
                return;
            }

            ReturnToMainMenuAsync().Forget();
        }

        private async UniTask ReturnToMainMenuAsync()
        {
            _mainMenuButton.interactable = false;

            try
            {
                await _sceneLoader.LoadAsync(
                    _mainMenuSceneName,
                    this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);

                if (_mainMenuButton != null)
                {
                    _mainMenuButton.interactable = true;
                }
            }
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
