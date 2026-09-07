using System;

namespace ProjectSparks.Companions
{
    public sealed class MechanicCompanionAbility : ICompanionAbility
    {
        private const float _boostConsumptionMultiplier = 0.75f;

        public CompanionType CompanionType => CompanionType.Mechanic;

        public float ModifyPickupRadius(float baseRadius)
        {
            return Math.Max(0f, baseRadius);
        }

        public float ModifyBoostConsumption(float baseConsumption)
        {
            return Math.Max(0f, baseConsumption) * _boostConsumptionMultiplier;
        }
    }
}
