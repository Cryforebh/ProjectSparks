using UnityEngine;
using Zenject;

namespace ProjectSparks.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionTargetView : MonoBehaviour
    {
        [Tooltip("Список складов и мест доставки этой сцены.")]
        [SerializeField] private MissionScenePoints _scenePoints;
        [Tooltip("Единственный маркер, заранее размещенный в этой сцене. Изначально выключен.")]
        [SerializeField] private MissionTargetMarker _marker;
        [Tooltip("Высота маркера над верхней границей триггера зоны в метрах.")]
        [SerializeField, Min(0f)] private float _surfaceOffset = 0.04f;

        private IMissionService _missionService;
        private MissionScenePoint _currentPoint;
        private bool _isInitialized;
        private bool _isSubscribed;

        public MissionScenePoint CurrentPoint => _currentPoint;

        [Inject]
        private void Construct(IMissionService missionService)
        {
            _missionService = missionService;
        }

        private void Start()
        {
            if (_scenePoints == null || _marker == null ||
                _scenePoints.gameObject.scene != gameObject.scene || _marker.gameObject.scene != gameObject.scene ||
                transform.IsChildOf(_marker.transform))
            {
                Debug.LogError("В MissionTargetView назначьте список точек и отдельный маркер из этой сцены.", this);
                enabled = false;
                return;
            }

            _isInitialized = true;
            HideMarker();
            Subscribe();
        }

        private void OnEnable()
        {
            if (_isInitialized)
                Subscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed || _missionService == null)
                return;
            _missionService.StateChanged += Refresh;
            _isSubscribed = true;
            Refresh();
        }

        private void OnDisable()
        {
            if (_isSubscribed)
            {
                _missionService.StateChanged -= Refresh;
                _isSubscribed = false;
            }

            if (_isInitialized)
                HideMarker();
        }

        private void HideMarker()
        {
            if (_marker != null)
                _marker.gameObject.SetActive(false);
            _currentPoint = null;
        }

        private void Refresh()
        {
            MissionAssignment mission = _missionService.ActiveMission;
            MissionScenePoint point = null;
            if (mission != null)
            {
                if (_missionService.Phase == MissionPhase.AwaitingPickup)
                    point = _scenePoints.FindPoint(mission.PickupPointId, MissionPointKind.Pickup);
                else if (_missionService.Phase == MissionPhase.Delivering)
                    point = _scenePoints.FindPoint(mission.DeliveryPointId, MissionPointKind.Delivery);
            }

            if (point == null)
            {
                HideMarker();
                return;
            }

            if (_currentPoint != point)
                _marker.gameObject.SetActive(false);
            _marker.transform.SetPositionAndRotation(point.SurfacePosition + Vector3.up * _surfaceOffset, Quaternion.identity);
            _currentPoint = point;
            _marker.gameObject.SetActive(true);
        }
    }
}
