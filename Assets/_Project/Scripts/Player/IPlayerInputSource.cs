using UnityEngine;

namespace ProjectSparks.Player
{
    public interface IPlayerInputSource
    {
        Vector2 Move { get; }
        Vector2 Look { get; }

        bool ConsumeInteract();
    }
}
