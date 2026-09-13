using System;

namespace ProjectSparks.Companions
{
    public readonly struct SparkAbilityEffects
    {
        public SparkAbilityEffects(float pickupRadiusMultiplier, float boostConsumptionMultiplier)
        {
            PickupRadiusMultiplier = pickupRadiusMultiplier;
            BoostConsumptionMultiplier = boostConsumptionMultiplier;
        }

        public float PickupRadiusMultiplier { get; }
        public float BoostConsumptionMultiplier { get; }
    }

    public sealed class SparkDevelopmentCalculator
    {
        private readonly SparkBalanceSettings _settings;

        public SparkDevelopmentCalculator(SparkBalanceSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (!settings.TryValidate(out string error))
                throw new ArgumentException(error, nameof(settings));
            _settings = settings;
        }

        public SparkAbilityEffects Evaluate(SparkStatValues stats)
        {
            return new SparkAbilityEffects(
                1f + _settings.PickupRadius.Evaluate(stats),
                1f - _settings.BoostSaving.Evaluate(stats));
        }
    }
}
