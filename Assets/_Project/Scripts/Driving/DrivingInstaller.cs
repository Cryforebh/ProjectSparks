using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Driving
{
    public sealed class DrivingInstaller : MonoInstaller
    {
        [Tooltip("Компонент, который передает управление машине.")]
        [SerializeField] private ArcadeCarInput _vehicleInput;
        [Tooltip("Контроллер, который предоставляет данные машины игровым системам.")]
        [SerializeField] private ArcadeCarController _vehicleController;

        public override void InstallBindings()
        {
            if (_vehicleInput == null || _vehicleController == null)
            {
                throw new ZenjectException("DrivingInstaller: не назначены компоненты машины.");
            }

            Container
                .Bind<IVehicleInputSource>()
                .FromInstance(_vehicleInput)
                .AsSingle();

            Container
                .Bind<IPauseInputSource>()
                .FromInstance(_vehicleInput)
                .AsSingle();

            Container
                .Bind<IVehicleTelemetry>()
                .FromInstance(_vehicleController)
                .AsSingle();
        }
    }
}
