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
