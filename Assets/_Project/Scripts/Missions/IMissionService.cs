using System;

namespace ProjectSparks.Missions
{
    public interface IMissionService
    {
        event Action StateChanged;

        MissionPhase Phase { get; }
        MissionAssignment ActiveMission { get; }

        bool TryStart(MissionAssignment mission);
        bool TryPickup(string pointId);
        bool TryComplete(string pointId);
        void Reset();
    }
}
