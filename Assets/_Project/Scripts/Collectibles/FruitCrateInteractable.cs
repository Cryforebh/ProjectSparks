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
        [Header("Стартовые фрукты новой игры")]
        [SerializeField, Min(0), InspectorName("Плавание")] private int _startingSwimFruits;
        [SerializeField, Min(0), InspectorName("Полёт")] private int _startingFlyFruits;
        [SerializeField, Min(0), InspectorName("Бег")] private int _startingRunFruits;
        [SerializeField, Min(0), InspectorName("Сила")] private int _startingPowerFruits;
        [SerializeField, Min(0), InspectorName("Выносливость")] private int _startingStaminaFruits;

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
            _progressService.TryInitializeStarterFruits(
                _startingSwimFruits,
                _startingFlyFruits,
                _startingRunFruits,
                _startingPowerFruits,
                _startingStaminaFruits);
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
