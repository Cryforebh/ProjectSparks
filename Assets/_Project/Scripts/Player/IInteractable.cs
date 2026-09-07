namespace ProjectSparks.Player
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract { get; }

        void Interact();
    }
}
