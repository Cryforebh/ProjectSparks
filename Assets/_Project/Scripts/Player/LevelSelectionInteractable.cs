using ProjectSparks.Levels;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class LevelSelectionInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Подсказка при наведении на выход для доставки.")]
        [SerializeField] private string _interactionPrompt = "Нажмите E, чтобы выбрать локацию";

        private LevelSelectionService _selection;
        public string InteractionPrompt => _interactionPrompt;
        public bool CanInteract => _selection != null && !_selection.IsBusy;

        [Inject]
        private void Construct(LevelSelectionService selection) => _selection = selection;

        public void Interact()
        {
            if (CanInteract)
                _selection.Open();
        }
    }
}
