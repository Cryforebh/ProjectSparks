using System;

namespace ProjectSparks.Core
{
    public interface IPauseService
    {
        event Action<bool> PauseChanged;

        bool IsPaused { get; }

        void SetPaused(bool isPaused);
        void TogglePause();
    }
}
