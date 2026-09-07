using UnityEngine;

namespace ProjectSparks.Player
{
    public interface IHoldableItem
    {
        HeldItemType ItemType { get; }
        Transform ItemTransform { get; }

        void SetHeld(bool isHeld);
    }
}
