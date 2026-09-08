using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using ProjectSparks.Levels;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuView : MonoBehaviour
    {
        [Tooltip("Кнопка входа в убежище.")]
        [SerializeField] private Button _playButton;
        [Tooltip("Кнопка открытия выбора локации для доставки.")]
        [SerializeField] private Button _tripButton;
        [Tooltip("Кнопка выхода из игры.")]
        [SerializeField] private Button _exitButton;
        [Tooltip("Имя сцены, которая открывается кнопкой запуска игры.")]
        [SerializeField] private string _gameSceneName = "Shelter";

        private ISceneLoader _sceneLoader;
        private LevelSelectionService _levelSelection;

        [Inject]
        private void Construct(ISceneLoader sceneLoader, LevelSelectionService levelSelection)
        {
            _sceneLoader = sceneLoader;
            _levelSelection = levelSelection;
        }

        private void Start()
        {
            _playButton.onClick.AddListener(HandlePlayClicked);
            _tripButton.onClick.AddListener(_levelSelection.Open);
            _exitButton.onClick.AddListener(HandleExitClicked);
        }

        private void OnDestroy()
        {
            if (_tripButton != null)
                _tripButton.onClick.RemoveListener(_levelSelection.Open);
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
            _tripButton.interactable = false;

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
                    _tripButton.interactable = true;
                }
            }
        }
    }
}
