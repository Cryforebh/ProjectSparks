using System;
using ProjectSparks.Collectibles;
using ProjectSparks.Companions;

namespace ProjectSparks.Player
{
    public sealed class ShelterInteractionRequestService : IShelterInteractionRequestService
    {
        public event Action FruitCrateRequested;
        public event Action<ISparkProfile, IFruitItem> SparkFeedingRequested;

        public void RequestFruitCrate()
        {
            FruitCrateRequested?.Invoke();
        }

        public void RequestSparkFeeding(ISparkProfile spark, IFruitItem fruit)
        {
            if (spark != null && fruit != null)
            {
                SparkFeedingRequested?.Invoke(spark, fruit);
            }
        }
    }
}
