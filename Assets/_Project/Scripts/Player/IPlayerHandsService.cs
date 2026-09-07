using System;

namespace ProjectSparks.Player
{
    public interface IPlayerHandsService
    {
        event Action<IHoldableItem> HeldItemChanged;

        IHoldableItem HeldItem { get; }
        bool HasItem { get; }

        bool TryHold(IHoldableItem item);
        bool TryRelease();
    }
}
