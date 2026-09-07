using ProjectSparks.Collectibles;
using TMPro;
using UnityEngine;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class ResourceHudView : MonoBehaviour
    {
        [Tooltip("Текст количества собранных фруктов.")]
        [SerializeField] private TMP_Text _fruitCountText;
        [Tooltip("Текст количества найденных капсул.")]
        [SerializeField] private TMP_Text _capsuleCountText;

        private IResourceInventoryService _inventoryService;

        [Inject]
        private void Construct(IResourceInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        private void Start()
        {
            if (_fruitCountText == null || _capsuleCountText == null)
            {
                Debug.LogError("В ResourceHudView назначены не все текстовые поля.", this);
                enabled = false;
                return;
            }

            _inventoryService.AmountChanged += HandleAmountChanged;
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_inventoryService != null)
            {
                _inventoryService.AmountChanged -= HandleAmountChanged;
            }
        }

        private void HandleAmountChanged(ResourceType resourceType, int amount)
        {
            switch (resourceType)
            {
                case ResourceType.Fruit:
                    _fruitCountText.text = $"Фрукты: {amount}";
                    break;

                case ResourceType.Capsule:
                    _capsuleCountText.text = $"Капсулы: {amount}";
                    break;
            }
        }

        private void RefreshAll()
        {
            _fruitCountText.text = $"Фрукты: {_inventoryService.FruitCount}";
            _capsuleCountText.text = $"Капсулы: {_inventoryService.CapsuleCount}";
        }
    }
}
