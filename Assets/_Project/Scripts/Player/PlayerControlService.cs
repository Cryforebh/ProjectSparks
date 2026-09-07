using System;

namespace ProjectSparks.Player
{
    public sealed class PlayerControlService : IPlayerControlService
    {
        public event Action<bool> ControlEnabledChanged;

        public bool IsControlEnabled { get; private set; } = true;

        public void SetControlEnabled(bool isEnabled)
        {
            if (IsControlEnabled == isEnabled)
            {
                return;
            }

            IsControlEnabled = isEnabled;
            ControlEnabledChanged?.Invoke(IsControlEnabled);
        }
    }
}
