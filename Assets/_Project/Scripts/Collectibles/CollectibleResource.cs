using ProjectSparks.Missions;
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

        private ITripSessionService _tripSession;
        private bool _isCollected;

        [Inject]
        private void Construct(ITripSessionService tripSession)
        {
            _tripSession = tripSession;
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
            bool added = _resourceType switch
            {
                ResourceType.Fruit => _tripSession.TryAddFruit(_fruitType, _amount),
                ResourceType.Capsule => _tripSession.TryAddCapsules(_amount),
                _ => false
            };

            if (!added)
            {
                _isCollected = false;
                return false;
            }

            gameObject.SetActive(false);
            return true;
        }
    }
}
