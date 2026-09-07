using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class SceneTransitionButtonView : MonoBehaviour
    {
        [Tooltip("Кнопка перехода в другую сцену.")]
        [SerializeField] private Button _button;
        [Tooltip("Имя сцены, которая открывается кнопкой.")]
        [SerializeField] private string _destinationSceneName;
        [Tooltip("Идентификатор точки появления в следующей сцене.")]
        [SerializeField] private string _destinationSpawnPointId;

        private ISceneTransitionService _transitionService;

        [Inject]
        private void Construct(ISceneTransitionService transitionService)
        {
            _transitionService = transitionService;
        }

        private void Start()
        {
            if (_button == null)
            {
                Debug.LogError("В SceneTransitionButtonView не назначена кнопка.", this);
                enabled = false;
                return;
            }

            _button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }
        }

        private void HandleClicked()
        {
            if (!_transitionService.IsTransitioning)
            {
                TransitionAsync().Forget();
            }
        }

        private async UniTask TransitionAsync()
        {
            _button.interactable = false;

            try
            {
                await _transitionService.TransitionAsync(
                    _destinationSceneName,
                    _destinationSpawnPointId,
                    this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);

                if (_button != null)
                {
                    _button.interactable = true;
                }
            }
        }
    }
}
