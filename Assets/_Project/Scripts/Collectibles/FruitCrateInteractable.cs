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
            IShelterInteractionRequestService requestService)
        {
            _handsService = handsService;
            _fruitWorldService = fruitWorldService;
            _requestService = requestService;
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
