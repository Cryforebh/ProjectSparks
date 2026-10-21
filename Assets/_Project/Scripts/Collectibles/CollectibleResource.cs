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
        [Min(1), Tooltip("Количество выбранного ресурса за подбор этого объекта.")]
        [SerializeField] private int _amount = 1;
        [Tooltip("Выбранный фрукт из каталога. При подборе начисляется его ID, внешний вид этого объекта не меняется. Используется только для ресурса Fruit.")]
        [SerializeField] private PhysicalFruitItem _fruitPrefab;

        private ITripSessionService _tripSession;
        private bool _isCollected;

        public ResourceType ResourceType => _resourceType;

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
                ResourceType.Fruit => _fruitPrefab != null &&
                    _tripSession.TryAddFruit(_fruitPrefab.FruitId, _amount),
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
