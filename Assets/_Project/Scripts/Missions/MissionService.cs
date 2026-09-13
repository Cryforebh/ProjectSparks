using System;

namespace ProjectSparks.Missions
{
    public sealed class MissionService : IMissionService
    {
        private readonly MissionState _inactiveState = new InactiveMissionState();
        private readonly MissionState _awaitingPickupState = new AwaitingPickupMissionState();
        private readonly MissionState _deliveringState = new DeliveringMissionState();
        private readonly MissionState _completedState = new CompletedMissionState();
        private readonly ITripSessionService _tripSession;

        private MissionState _currentState;

        public MissionService(ITripSessionService tripSession)
        {
            _tripSession = tripSession;
            _currentState = _inactiveState;
        }

        public event Action StateChanged;

        public MissionPhase Phase => _currentState.Phase;
        public MissionAssignment ActiveMission { get; private set; }

        public bool TryStart(MissionAssignment mission)
        {
            return mission != null && _currentState.TryStart(this, mission);
        }

        public bool TryPickup(string pointId)
        {
            return !string.IsNullOrWhiteSpace(pointId) && _currentState.TryPickup(this, pointId);
        }

        public bool TryComplete(string pointId)
        {
            return !string.IsNullOrWhiteSpace(pointId) && _currentState.TryComplete(this, pointId);
        }

        public void Reset()
        {
            ActiveMission = null;
            TransitionTo(_inactiveState);
        }

        internal bool IsPickupPoint(string pointId) => ActiveMission != null &&
            string.Equals(ActiveMission.PickupPointId, pointId, StringComparison.Ordinal);

        internal bool IsDeliveryPoint(string pointId) => ActiveMission != null &&
            string.Equals(ActiveMission.DeliveryPointId, pointId, StringComparison.Ordinal);

        internal bool BeginMission(MissionAssignment mission)
        {
            if (!_tripSession.TryBeginDelivery(mission.Id))
            {
                return false;
            }

            ActiveMission = mission;
            TransitionTo(_awaitingPickupState);
            return true;
        }

        internal void MarkCargoPickedUp()
        {
            TransitionTo(_deliveringState);
        }

        internal bool MarkMissionCompleted()
        {
            if (!ReferenceEquals(_currentState, _deliveringState) ||
                !_tripSession.TryCompleteDelivery(ActiveMission.Id, ActiveMission.Reward))
            {
                return false;
            }

            _currentState = _completedState;
            StateChanged?.Invoke();
            return true;
        }

        private void TransitionTo(MissionState nextState)
        {
            if (ReferenceEquals(_currentState, nextState))
            {
                return;
            }

            _currentState = nextState;
            StateChanged?.Invoke();
        }
    }
}
