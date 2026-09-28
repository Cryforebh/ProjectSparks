using ProjectSparks.Player;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    public sealed class SparkEditingTable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform _seat;

        private IPlayerHandsService _hands;
        private IShelterInteractionRequestService _requests;

        public string InteractionPrompt => "Посадить Искру за стол";
        public bool CanInteract => _seat != null && _hands?.HeldItem is SparkInteractable;

        [Inject]
        private void Construct(IPlayerHandsService hands,
            IShelterInteractionRequestService requests)
        {
            _hands = hands;
            _requests = requests;
        }

        public void Interact()
        {
            if (CanInteract)
            {
                _requests.RequestSparkEditing((SparkInteractable)_hands.HeldItem, _seat);
            }
        }
    }
}
