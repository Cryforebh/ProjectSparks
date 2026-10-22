using ProjectSparks.Core;
using ProjectSparks.Companions;
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
        [Tooltip("Камера, которая выводит игровую сцену. Назначьте Camera с Cinemachine Brain, а не виртуальную Cinemachine Camera.")]
        [SerializeField] private Camera _viewCamera;

        public override void InstallBindings()
        {
            if (_vehicleInput == null || _vehicleController == null)
            {
                throw new ZenjectException("DrivingInstaller: не назначены компоненты машины.");
            }

            if (_viewCamera == null)
            {
                throw new ZenjectException("DrivingInstaller: не назначена игровая камера.");
            }
            Container.Bind<Camera>().FromInstance(_viewCamera).AsSingle();

            PassengerSeat seat = _vehicleController.GetComponent<PassengerSeat>();
            if (seat == null || seat.Point == null || !seat.Point.IsChildOf(_vehicleController.transform))
                throw new ZenjectException("DrivingInstaller: на машине назначьте PassengerSeat с дочерней точкой.");
            Container.Bind<PassengerSeat>().FromInstance(seat).AsSingle();
            Container.BindInterfacesTo<TripSparkPresenter>().AsSingle();

            VehicleCargoPoints cargoPoints = _vehicleController.GetComponent<VehicleCargoPoints>();
            if (cargoPoints == null)
            {
                throw new ZenjectException("DrivingInstaller: на машине нет VehicleCargoPoints.");
            }

            Container
                .Bind<VehicleCargoPoints>()
                .FromInstance(cargoPoints)
                .AsSingle();

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
