using System;

namespace ProjectSparks.Companions
{
    public interface ICompanionService
    {
        event Action<CompanionType> SelectedCompanionChanged;

        CompanionType SelectedCompanion { get; }

        bool Select(CompanionType companionType);
        void Restore(CompanionType companionType);
        float ModifyPickupRadius(float baseRadius);
        float ModifyBoostConsumption(float baseConsumption);
    }
}
