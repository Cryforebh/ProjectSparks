using ProjectSparks.Collectibles;
using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class ShelterInstaller : MonoInstaller
    {
        [Tooltip("Компонент, который передает управление персонажу.")]
        [SerializeField] private FirstPersonPlayerInput _playerInput;
        [Tooltip("Компонент, который определяет доступное взаимодействие.")]
        [SerializeField] private PlayerInteractor _playerInteractor;
        [Tooltip("Компонент, который управляет предметом в руках персонажа.")]
        [SerializeField] private PlayerHandsController _playerHands;
        [Tooltip("Компонент, который управляет физическими фруктами и их пулом.")]
        [SerializeField] private FruitWorldController _fruitWorld;

        public override void InstallBindings()
        {
            if (_playerInput == null
                || _playerInteractor == null
                || _playerHands == null
                || _fruitWorld == null)
            {
                throw new ZenjectException("ShelterInstaller: не назначены компоненты игрока.");
            }

            Container
                .Bind<IPlayerInputSource>()
                .FromInstance(_playerInput)
                .AsSingle();

            Container
                .Bind<IPauseInputSource>()
                .FromInstance(_playerInput)
                .AsSingle();

            Container
                .Bind<IInteractionPromptSource>()
                .FromInstance(_playerInteractor)
                .AsSingle();

            Container
                .Bind<IPlayerHandsService>()
                .FromInstance(_playerHands)
                .AsSingle();

            Container
                .Bind<IFruitWorldService>()
                .FromInstance(_fruitWorld)
                .AsSingle();

            Container
                .Bind<IPlayerControlService>()
                .To<PlayerControlService>()
                .AsSingle();

            Container
                .Bind<IShelterInteractionRequestService>()
                .To<ShelterInteractionRequestService>()
                .AsSingle();

            Container
                .BindInterfacesAndSelfTo<PauseService>()
                .AsSingle()
                .NonLazy();
        }
    }
}
