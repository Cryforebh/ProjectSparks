using ProjectSparks.Companions;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Collectibles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ResourcePickupSensor : MonoBehaviour
    {
        [Tooltip("Радиус подбора ресурсов без способности компаньона.")]
        [SerializeField, Min(0.1f)] private float _basePickupRadius = 1.5f;
        [Tooltip("Слои объектов, которые можно подобрать.")]
        [SerializeField] private LayerMask _collectibleMask = 1 << 10;

        private ICompanionService _companionService;
        private SphereCollider _sensorCollider;

        [Inject]
        private void Construct(ICompanionService companionService)
        {
            _companionService = companionService;
        }

        private void Awake()
        {
            _sensorCollider = GetComponent<SphereCollider>();
            _sensorCollider.isTrigger = true;
        }

        private void Start()
        {
            _companionService.SelectedCompanionChanged += HandleSelectedCompanionChanged;
            ApplyPickupRadius();
        }

        private void OnDestroy()
        {
            if (_companionService != null)
            {
                _companionService.SelectedCompanionChanged -= HandleSelectedCompanionChanged;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((_collectibleMask.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            CollectibleResource collectible = other.GetComponentInParent<CollectibleResource>();
            if (collectible != null)
            {
                collectible.TryCollect();
            }
        }

        private void HandleSelectedCompanionChanged(CompanionType companionType)
        {
            ApplyPickupRadius();
        }

        private void ApplyPickupRadius()
        {
            _sensorCollider.radius = _companionService.ModifyPickupRadius(_basePickupRadius);
        }

        private void OnDrawGizmosSelected()
        {
            float radius = Application.isPlaying && _sensorCollider != null
                ? _sensorCollider.radius
                : _basePickupRadius;

            Gizmos.color = new Color(0.35f, 1f, 0.55f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
