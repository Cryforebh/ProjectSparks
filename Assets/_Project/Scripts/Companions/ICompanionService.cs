using System;

namespace ProjectSparks.Companions
{
    public interface ICompanionService
    {
        event Action<string> SelectedCompanionChanged;

        string SelectedCompanion { get; }

        bool Select(string sparkId);
        void Restore(string sparkId);
        float ModifyPickupRadius(float baseRadius);
        float ModifyBoostConsumption(float baseConsumption);
    }
}
