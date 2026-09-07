using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class SceneDoorInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Подсказка, которая отображается при наведении на дверь.")]
        [SerializeField] private string _interactionPrompt = "Нажмите E, чтобы войти";
        [Tooltip("Имя сцены, которая открывается этой дверью.")]
        [SerializeField] private string _destinationSceneName = "Garage";
        [Tooltip("Идентификатор точки появления в следующей сцене.")]
        [SerializeField] private string _destinationSpawnPointId = "GarageEntrance";

        private ISceneTransitionService _transitionService;
        private bool _isInteracting;

        public string InteractionPrompt => _interactionPrompt;
        public bool CanInteract => !_isInteracting && !_transitionService.IsTransitioning;

        [Inject]
        private void Construct(ISceneTransitionService transitionService)
        {
            _transitionService = transitionService;
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            TransitionAsync().Forget();
        }

        private async UniTask TransitionAsync()
        {
            _isInteracting = true;

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
            }
            finally
            {
                _isInteracting = false;
            }
        }
    }
}
