using System;

namespace ProjectSparks.Player
{
    public interface IPlayerControlService
    {
        event Action<bool> ControlEnabledChanged;

        bool IsControlEnabled { get; }

        void SetControlEnabled(bool isEnabled);
    }
}
