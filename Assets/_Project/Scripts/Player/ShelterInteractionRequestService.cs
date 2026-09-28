using System;
using ProjectSparks.Collectibles;
using ProjectSparks.Companions;
using UnityEngine;

namespace ProjectSparks.Player
{
    public sealed class ShelterInteractionRequestService : IShelterInteractionRequestService
    {
        public event Action FruitCrateRequested;
        public event Action<ISparkProfile, IFruitItem> SparkFeedingRequested;
        public event Action<SparkInteractable, Transform> SparkEditingRequested;

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

        public void RequestSparkEditing(SparkInteractable spark, Transform seat)
        {
            if (spark != null && seat != null)
            {
                SparkEditingRequested?.Invoke(spark, seat);
            }
        }
    }
}
