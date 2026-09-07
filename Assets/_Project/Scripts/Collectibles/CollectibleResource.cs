using UnityEngine;
using Zenject;

namespace ProjectSparks.Collectibles
{
    [DisallowMultipleComponent]
    public sealed class CollectibleResource : MonoBehaviour
    {
        [Tooltip("Тип ресурса, который получит игрок.")]
        [SerializeField] private ResourceType _resourceType;
        [Tooltip("Количество ресурса за один объект.")]
        [SerializeField, Min(1)] private int _amount = 1;
        [Tooltip("Тип фрукта. Используется только для ресурса Fruit.")]
        [SerializeField] private FruitType _fruitType = FruitType.Fly;

        private IResourceInventoryService _inventoryService;
        private bool _isCollected;

        [Inject]
        private void Construct(IResourceInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        private void OnEnable()
        {
            _isCollected = false;
        }

        public bool TryCollect()
        {
            if (_isCollected)
            {
                return false;
            }

            _isCollected = true;
            if (_resourceType == ResourceType.Fruit)
            {
                _inventoryService.AddFruit(_fruitType, _amount);
            }
            else
            {
                _inventoryService.Add(_resourceType, _amount);
            }
            gameObject.SetActive(false);
            return true;
        }
    }
}
