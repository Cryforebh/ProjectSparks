using ProjectSparks.Core;
using ProjectSparks.UI;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Infrastructure
{
    [DisallowMultipleComponent]
    public sealed class SceneTransitionInstaller : MonoInstaller
    {
        [Tooltip("Постоянный экран затемнения переходов между сценами.")]
        [SerializeField] private SceneFadeView _fadeView;

        public override void InstallBindings()
        {
            if (_fadeView == null)
            {
                throw new ZenjectException("SceneTransitionInstaller: не назначен экран затемнения.");
            }

            Container
                .Bind<SceneFadeView>()
                .FromInstance(_fadeView)
                .AsSingle();

            Container
                .Bind<ISceneEntryPointService>().To<SceneEntryPointService>()
                .AsSingle();

            Container
                .BindInterfacesAndSelfTo<SceneTransitionService>()
                .AsSingle();
        }
    }
}
