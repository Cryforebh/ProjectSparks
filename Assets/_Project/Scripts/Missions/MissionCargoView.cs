using System;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionCargoView : MonoBehaviour
    {
        [Tooltip("Миссия, которой принадлежит этот груз.")]
        [SerializeField] private MissionDefinition _mission;
        [Tooltip("Объекты груза, показываемые во время доставки.")]
        [SerializeField] private GameObject[] _cargoVisuals = Array.Empty<GameObject>();

        private IMissionService _missionService;
        private bool _isSubscribed;

        [Inject]
        public void Construct(IMissionService missionService)
        {
            _missionService = missionService;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed || _missionService == null)
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
            bool shouldShow = ReferenceEquals(_missionService.ActiveMission, _mission)
                && _missionService.Phase == MissionPhase.Delivering;

            foreach (GameObject cargoVisual in _cargoVisuals)
            {
                if (cargoVisual != null && cargoVisual.activeSelf != shouldShow)
                {
                    cargoVisual.SetActive(shouldShow);
                }
            }
        }
    }
}
