using System;

namespace ProjectSparks.Missions
{
    public interface IMissionService
    {
        event Action StateChanged;

        MissionPhase Phase { get; }
        MissionDefinition ActiveMission { get; }

        bool TryStart(MissionDefinition mission);
        bool TryPickup(MissionDefinition mission);
        bool TryComplete(MissionDefinition mission);
        void Reset();
    }
}
