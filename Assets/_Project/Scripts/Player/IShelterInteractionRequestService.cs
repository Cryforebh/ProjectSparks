using System;
using ProjectSparks.Collectibles;
using ProjectSparks.Companions;

namespace ProjectSparks.Player
{
    public interface IShelterInteractionRequestService
    {
        event Action FruitCrateRequested;
        event Action<ISparkProfile, IFruitItem> SparkFeedingRequested;

        void RequestFruitCrate();
        void RequestSparkFeeding(ISparkProfile spark, IFruitItem fruit);
    }
}
