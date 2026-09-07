using System;

namespace ProjectSparks.Player
{
    public interface IInteractionPromptSource
    {
        event Action<string> PromptChanged;

        string CurrentPrompt { get; }
    }
}
