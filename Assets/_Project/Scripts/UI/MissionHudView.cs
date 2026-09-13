using ProjectSparks.Missions;
using TMPro;
using UnityEngine;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class MissionHudView : MonoBehaviour
    {
        [Tooltip("Панель с информацией о текущей миссии.")]
        [SerializeField] private GameObject _missionPanel;
        [Tooltip("Текст названия текущей миссии.")]
        [SerializeField] private TMP_Text _titleText;
        [Tooltip("Текст текущей цели миссии.")]
        [SerializeField] private TMP_Text _objectiveText;

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
            MissionAssignment mission = _missionService.ActiveMission;
            bool hasMission = mission != null && _missionService.Phase != MissionPhase.Inactive;

            SetPanelVisible(hasMission);
            if (!hasMission)
            {
                return;
            }

            if (_titleText != null)
            {
                _titleText.text = mission.Title;
            }

            if (_objectiveText != null)
            {
                _objectiveText.text = GetObjectiveText(mission);
            }
        }

        private string GetObjectiveText(MissionAssignment mission)
        {
            switch (_missionService.Phase)
            {
                case MissionPhase.AwaitingPickup:
                    return $"Заберите груз: {mission.PickupLabel}";

                case MissionPhase.Delivering:
                    return $"Доставьте груз: {mission.DestinationLabel}";

                case MissionPhase.Completed:
                    return $"Доставка выполнена. Награда: {mission.Reward}";

                default:
                    return string.Empty;
            }
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (_missionPanel != null && _missionPanel.activeSelf != isVisible)
            {
                _missionPanel.SetActive(isVisible);
            }
        }
    }
}
