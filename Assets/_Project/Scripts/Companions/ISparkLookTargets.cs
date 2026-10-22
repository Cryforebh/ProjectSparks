using System.Collections.Generic;
using ProjectSparks.Player;
using UnityEngine;

namespace ProjectSparks.Companions
{
    public interface ISparkLookTargets
    {
        Transform PlayerTarget { get; }
        IReadOnlyList<SparkInteractable> Sparks { get; }

        void RegisterPlayer(PlayerLookTarget player);
        void UnregisterPlayer(PlayerLookTarget player);
        void RegisterSpark(SparkInteractable spark);
        void UnregisterSpark(SparkInteractable spark);
    }
}
