using ProjectSparks.Collectibles;
using ProjectSparks.Companions;
using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Infrastructure
{
    [DisallowMultipleComponent]
    public sealed class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .Bind<ICompanionAbility>().To<CollectorCompanionAbility>()
                .AsSingle();

            Container
                .Bind<ICompanionAbility>().To<MechanicCompanionAbility>()
                .AsSingle();

            Container
                .Bind<ICompanionService>().To<CompanionService>()
                .AsSingle();

            Container
                .Bind<ISparkProgressService>().To<SparkProgressService>()
                .AsSingle();

            Container
                .Bind<ISparkFeedingService>().To<SparkFeedingService>()
                .AsSingle();

            Container
                .Bind<IResourceInventoryService>().To<ResourceInventoryService>()
                .AsSingle();

            Container
                .Bind<IEconomyService>().To<EconomyService>()
                .AsSingle();

            Container
                .Bind<ISaveService>().To<JsonFileSaveService>()
                .AsSingle()
                .WithArguments(Application.persistentDataPath);

            Container
                .BindInterfacesAndSelfTo<ProgressService>()
                .AsSingle();

            Container
                .Bind<ISceneLoader>().To<SceneLoader>()
                .AsSingle();
        }
    }
}
