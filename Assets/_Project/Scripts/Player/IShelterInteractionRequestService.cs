using System;
using ProjectSparks.Collectibles;
using ProjectSparks.Companions;
using UnityEngine;

namespace ProjectSparks.Player
{
    public interface IShelterInteractionRequestService
    {
        event Action FruitCrateRequested;
        event Action<ISparkProfile, IFruitItem> SparkFeedingRequested;
        event Action<SparkInteractable, Transform> SparkEditingRequested;

        void RequestFruitCrate();
        void RequestSparkFeeding(ISparkProfile spark, IFruitItem fruit);
        void RequestSparkEditing(SparkInteractable spark, Transform seat);
    }
}
