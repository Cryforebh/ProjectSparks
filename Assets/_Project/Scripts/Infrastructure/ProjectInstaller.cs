using ProjectSparks.Collectibles;
using ProjectSparks.Companions;
using ProjectSparks.Core;
using ProjectSparks.Levels;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Infrastructure
{
    [DisallowMultipleComponent]
    public sealed class ProjectInstaller : MonoInstaller
    {
        [Tooltip("Общий список локаций для главного меню и убежища.")]
        [SerializeField] private LevelCatalog _levelCatalog;
        [Tooltip("Общий каталог фруктов, их прибавок, иконок и стартовых количеств.")]
        [SerializeField] private FruitCatalog _fruitCatalog;
        [Tooltip("Общие настройки роста и способностей для всех Искр.")]
        [SerializeField] private SparkBalanceSettings _sparkBalance;

        [Tooltip("Общая модель Искры для всех сцен.")]
        [SerializeField] private SparkVisualSettings _sparkVisualSettings;
        [Tooltip("Стартовые обязательные Искры.")]
        [SerializeField] private SparkStarterSettings _sparkStarterSettings;

        public override void InstallBindings()
        {
            if (_sparkVisualSettings == null || _sparkVisualSettings.BasePrefab == null)
                throw new ZenjectException("В ProjectInstaller нужны настройки общего визуала Искры.");
            Container.Bind<SparkVisualSettings>().FromInstance(_sparkVisualSettings).AsSingle();
            Container.Bind<SparkVisualFactory>().AsSingle();
            if (_sparkBalance == null)
                throw new ZenjectException("В ProjectInstaller нужны Spark Balance Settings.");
            if (!_sparkBalance.TryValidate(out string balanceError))
                throw new ZenjectException(balanceError);
            Container.Bind<SparkBalanceSettings>().FromInstance(_sparkBalance).AsSingle();
            if (_fruitCatalog == null || _fruitCatalog.Count == 0)
                throw new ZenjectException("В ProjectInstaller нужен непустой Fruit Catalog.");
            if (!_fruitCatalog.TryValidate(out string fruitError))
                throw new ZenjectException(fruitError);
            Container.Bind<FruitCatalog>().FromInstance(_fruitCatalog).AsSingle();
            Container.Bind<SparkDevelopmentCalculator>().AsSingle();
            Container.Bind<LevelCatalog>().FromInstance(_levelCatalog).AsSingle();
            Container.Bind<LevelSelectionService>().AsSingle();

            Container
                .BindInterfacesTo<CompanionService>()
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

            Container.BindInstance(_sparkStarterSettings);
        }
    }
}
