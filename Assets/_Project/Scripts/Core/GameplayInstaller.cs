using ProjectSparks.Missions;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Core
{
    [DisallowMultipleComponent]
    public sealed class GameplayInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<LevelRestartService>().AsSingle().NonLazy();

            Container
                .BindInterfacesAndSelfTo<PauseService>()
                .AsSingle()
                .NonLazy();

            Container
                .Bind<ITripSessionService>().To<TripSessionService>()
                .AsSingle();

            Container
                .Bind<IMissionService>().To<MissionService>()
                .AsSingle();

            Container.Bind<TripCompletionService>().AsSingle();
            Container.Bind<TripExitService>().AsSingle();
        }
    }
}
