using System;
using ProjectSparks.Driving;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionCargoView : MonoBehaviour
    {
        [Tooltip("Префаб посылки, создаваемый в каждой точке груза текущей машины.")]
        [SerializeField] private GameObject _cargoPrefab;

        private IMissionService _missionService;
        private VehicleCargoPoints _cargoPoints;
        private GameObject[] _instances = Array.Empty<GameObject>();
        private bool _isSubscribed;

        [Inject]
        public void Construct(IMissionService missionService, VehicleCargoPoints cargoPoints)
        {
            _missionService = missionService;
            _cargoPoints = cargoPoints;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetCargoVisible(false);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ClearCargo();
        }

        private void Subscribe()
        {
            if (_isSubscribed || _missionService == null || !isActiveAndEnabled)
            {
                return;
            }

            _missionService.StateChanged += Refresh;
            _isSubscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _missionService.StateChanged -= Refresh;
            _isSubscribed = false;
        }

        private void Refresh()
        {
            bool shouldShow = _missionService.ActiveMission != null
                && _missionService.Phase == MissionPhase.Delivering;

            if (!shouldShow)
            {
                SetCargoVisible(false);
                return;
            }

            if (_instances.Length > 0)
            {
                SetCargoVisible(true);
                return;
            }

            if (_cargoPrefab == null || _cargoPoints == null || _cargoPoints.Count == 0)
            {
                Debug.LogError("Для груза не назначен префаб или точки на машине.", this);
                return;
            }

            for (int index = 0; index < _cargoPoints.Count; index++)
            {
                if (_cargoPoints.GetPoint(index) == null)
                {
                    Debug.LogError($"На машине не назначена точка груза {index + 1}.", this);
                    return;
                }
            }

            _instances = new GameObject[_cargoPoints.Count];
            for (int index = 0; index < _instances.Length; index++)
            {
                GameObject instance = Instantiate(_cargoPrefab, _cargoPoints.GetPoint(index), false);
                _instances[index] = instance;
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.SetActive(true);
            }
        }

        private void SetCargoVisible(bool isVisible)
        {
            foreach (GameObject instance in _instances)
            {
                if (instance != null && instance.activeSelf != isVisible)
                {
                    instance.SetActive(isVisible);
                }
            }
        }

        private void ClearCargo()
        {
            foreach (GameObject instance in _instances)
            {
                if (instance != null)
                {
                    instance.SetActive(false);
                    Destroy(instance);
                }
            }

            _instances = Array.Empty<GameObject>();
        }
    }
}
