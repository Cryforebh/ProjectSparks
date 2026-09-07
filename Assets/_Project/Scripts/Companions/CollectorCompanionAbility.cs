using System;

namespace ProjectSparks.Companions
{
    public sealed class CollectorCompanionAbility : ICompanionAbility
    {
        private const float _pickupRadiusMultiplier = 1.5f;

        public CompanionType CompanionType => CompanionType.Collector;

        public float ModifyPickupRadius(float baseRadius)
        {
            return Math.Max(0f, baseRadius) * _pickupRadiusMultiplier;
        }

        public float ModifyBoostConsumption(float baseConsumption)
        {
            return Math.Max(0f, baseConsumption);
        }
    }
}
