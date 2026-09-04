using System;
using ProjectSparks.Core;

namespace ProjectSparks.Missions
{
    public sealed class MissionService : IMissionService
    {
        private readonly MissionState _inactiveState = new InactiveMissionState();
        private readonly MissionState _awaitingPickupState = new AwaitingPickupMissionState();
        private readonly MissionState _deliveringState = new DeliveringMissionState();
        private readonly MissionState _completedState = new CompletedMissionState();
        private readonly IEconomyService _economyService;

        private MissionState _currentState;

        public MissionService(IEconomyService economyService)
        {
            _economyService = economyService;
            _currentState = _inactiveState;
        }

        public event Action StateChanged;

        public MissionPhase Phase => _currentState.Phase;
        public MissionDefinition ActiveMission { get; private set; }

        public bool TryStart(MissionDefinition mission)
        {
            return mission != null && _currentState.TryStart(this, mission);
        }

        public bool TryPickup(MissionDefinition mission)
        {
            return mission != null && _currentState.TryPickup(this, mission);
        }

        public bool TryComplete(MissionDefinition mission)
        {
            return mission != null && _currentState.TryComplete(this, mission);
        }

        public void Reset()
        {
            ActiveMission = null;
            TransitionTo(_inactiveState);
        }

        internal bool IsActiveMission(MissionDefinition mission)
        {
            return ReferenceEquals(ActiveMission, mission);
        }

        internal void BeginMission(MissionDefinition mission)
        {
            ActiveMission = mission;
            TransitionTo(_awaitingPickupState);
        }

        internal void MarkCargoPickedUp()
        {
            TransitionTo(_deliveringState);
        }

        internal void MarkMissionCompleted()
        {
            if (ReferenceEquals(_currentState, _completedState))
            {
                return;
            }

            _currentState = _completedState;
            _economyService.Add(ActiveMission.Reward);
            StateChanged?.Invoke();
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
