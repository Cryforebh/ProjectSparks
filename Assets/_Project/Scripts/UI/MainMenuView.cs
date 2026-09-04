using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuView : MonoBehaviour
    {
        [Tooltip("Кнопка запуска игры.")]
        [SerializeField] private Button _playButton;
        [Tooltip("Кнопка выхода из игры.")]
        [SerializeField] private Button _exitButton;
        [Tooltip("Имя сцены, которая открывается кнопкой запуска игры.")]
        [SerializeField] private string _gameSceneName = "DrivingSandbox";

        private ISceneLoader _sceneLoader;

        [Inject]
        private void Construct(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        private void Start()
        {
            _playButton.onClick.AddListener(HandlePlayClicked);
            _exitButton.onClick.AddListener(HandleExitClicked);
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(HandlePlayClicked);
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveListener(HandleExitClicked);
            }
        }

        private void HandlePlayClicked()
        {
            LoadGameAsync().Forget();
        }

        private void HandleExitClicked()
        {
            Application.Quit();
        }

        private async UniTask LoadGameAsync()
        {
            _playButton.interactable = false;

            try
            {
                await _sceneLoader.LoadAsync(
                    _gameSceneName,
                    this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);

                if (_playButton != null)
                {
                    _playButton.interactable = true;
                }
            }
        }
    }
}
