using System;
using UnityEngine;

namespace ProjectSparks.Player
{
    public interface IPlayerHandsService
    {
        event Action<IHoldableItem> HeldItemChanged;

        IHoldableItem HeldItem { get; }
        bool HasItem { get; }

        bool TryHold(IHoldableItem item);
        bool TryRelease();
        bool TryPutDownSpark(Vector3 position, Quaternion rotation);
        bool TrySeatSpark(Transform seat);
    }
}
