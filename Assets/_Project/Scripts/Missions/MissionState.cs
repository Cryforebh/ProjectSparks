namespace ProjectSparks.Missions
{
    internal abstract class MissionState
    {
        public abstract MissionPhase Phase { get; }

        public virtual bool TryStart(MissionService context, MissionDefinition mission)
        {
            return false;
        }

        public virtual bool TryPickup(MissionService context, MissionDefinition mission)
        {
            return false;
        }

        public virtual bool TryComplete(MissionService context, MissionDefinition mission)
        {
            return false;
        }
    }

    internal sealed class InactiveMissionState : MissionState
    {
        public override MissionPhase Phase => MissionPhase.Inactive;

        public override bool TryStart(MissionService context, MissionDefinition mission)
        {
            context.BeginMission(mission);
            return true;
        }
    }

    internal sealed class AwaitingPickupMissionState : MissionState
    {
        public override MissionPhase Phase => MissionPhase.AwaitingPickup;

        public override bool TryPickup(MissionService context, MissionDefinition mission)
        {
            if (!context.IsActiveMission(mission))
            {
                return false;
            }

            context.MarkCargoPickedUp();
            return true;
        }
    }

    internal sealed class DeliveringMissionState : MissionState
    {
        public override MissionPhase Phase => MissionPhase.Delivering;

        public override bool TryComplete(MissionService context, MissionDefinition mission)
        {
            if (!context.IsActiveMission(mission))
            {
                return false;
            }

            context.MarkMissionCompleted();
            return true;
        }
    }

    internal sealed class CompletedMissionState : MissionState
    {
        public override MissionPhase Phase => MissionPhase.Completed;

        public override bool TryStart(MissionService context, MissionDefinition mission)
        {
            context.BeginMission(mission);
            return true;
        }
    }
}
