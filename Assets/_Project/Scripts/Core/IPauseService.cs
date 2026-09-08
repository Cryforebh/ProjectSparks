using System;

namespace ProjectSparks.Core
{
    public interface IPauseService
    {
        event Action<bool> PauseChanged;

        bool IsPaused { get; }
        bool IsPauseMenuVisible { get; }

        void SetPaused(bool isPaused, bool showMenu = true);
        void TogglePause();
    }
}
