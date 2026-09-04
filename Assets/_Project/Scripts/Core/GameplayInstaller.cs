using UnityEngine;
using Zenject;

namespace ProjectSparks.Core
{
    [DisallowMultipleComponent]
    public sealed class GameplayInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<PauseService>()
                .AsSingle()
                .NonLazy();
        }
    }
}
