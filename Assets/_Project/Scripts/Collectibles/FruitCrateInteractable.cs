using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using ProjectSparks.Player;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Collectibles
{
    [DisallowMultipleComponent]
    public sealed class FruitCrateInteractable : MonoBehaviour, IInteractable
    {
        private IPlayerHandsService _handsService;
        private IFruitWorldService _fruitWorldService;
        private IShelterInteractionRequestService _requestService;
        private IProgressService _progressService;

        public string InteractionPrompt =>
            _handsService.HeldItem is IFruitItem
                ? "Положить фрукт обратно"
                : "Открыть ящик фруктов";
        public bool CanInteract => !_handsService.HasItem
            || _handsService.HeldItem is IFruitItem;

        [Inject]
        private void Construct(
            IPlayerHandsService handsService,
            IFruitWorldService fruitWorldService,
            IShelterInteractionRequestService requestService,
            IProgressService progressService)
        {
            _handsService = handsService;
            _fruitWorldService = fruitWorldService;
            _requestService = requestService;
            _progressService = progressService;
        }

        private void Start()
        {
            _progressService.ResetCompleted += ApplyStarterFruits;
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            try
            {
                await _progressService.LoadAsync(this.GetCancellationTokenOnDestroy());
                ApplyStarterFruits();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void ApplyStarterFruits()
        {
            _progressService.TryInitializeStarterFruits();
        }

        private void OnDestroy()
        {
            if (_progressService != null)
            {
                _progressService.ResetCompleted -= ApplyStarterFruits;
            }
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            if (_handsService.HeldItem is IFruitItem)
            {
                _fruitWorldService.TryReturnHeldFruitToCrate();
            }
            else
            {
                _requestService.RequestFruitCrate();
            }
        }
    }
}
