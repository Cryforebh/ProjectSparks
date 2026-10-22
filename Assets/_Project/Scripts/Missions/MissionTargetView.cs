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
        [Tooltip("Точка возвращения в убежище. Используется тем же маркером после всех доставок.")]
        [SerializeField] private Transform _returnPoint;
        [Tooltip("Высота маркера над верхней границей триггера зоны в метрах.")]
        [SerializeField, Min(0f)] private float _surfaceOffset = 0.04f;

        private IMissionService _missionService;
        private ITripSessionService _tripSession;
        private MissionScenePoint _currentPoint;
        private Vector3 _currentTargetPosition;
        private bool _hasTarget;
        private bool _isInitialized;
        private bool _isSubscribed;

        // Оставлено для совместимости с другими системами: при возврате это null.
        public MissionScenePoint CurrentPoint => _currentPoint;

        public bool TryGetTargetPosition(out Vector3 position)
        {
            position = _currentTargetPosition;
            return _hasTarget;
        }

        [Inject]
        private void Construct(IMissionService missionService, ITripSessionService tripSession)
        {
            _missionService = missionService;
            _tripSession = tripSession;
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
            if (_isSubscribed || _missionService == null || _tripSession == null)
                return;
            _missionService.StateChanged += Refresh;
            _tripSession.Changed += Refresh;
            _isSubscribed = true;
            Refresh();
        }

        private void OnDisable()
        {
            if (_isSubscribed)
            {
                _missionService.StateChanged -= Refresh;
                _tripSession.Changed -= Refresh;
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
            _hasTarget = false;
            _currentTargetPosition = Vector3.zero;
        }

        private void Refresh()
        {
            // Статус сессии важнее фазы отдельной доставки.
            if (_tripSession.State == TripSessionState.Succeeded ||
                _tripSession.State == TripSessionState.Confirmed)
            {
                if (_returnPoint == null)
                {
                    HideMarker();
                    return;
                }

                ShowTarget(_returnPoint.position, null);
                return;
            }

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

            ShowTarget(point.SurfacePosition, point);
        }

        private void ShowTarget(Vector3 position, MissionScenePoint point)
        {
            if (!_hasTarget || _currentPoint != point)
                _marker.gameObject.SetActive(false);

            _currentPoint = point;
            _currentTargetPosition = position;
            _hasTarget = true;
            _marker.transform.SetPositionAndRotation(position + Vector3.up * _surfaceOffset, Quaternion.identity);
            _marker.gameObject.SetActive(true);
        }
    }
}
