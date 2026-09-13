using UnityEngine;
using Zenject;

namespace ProjectSparks.Missions
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(MissionScenePoint))]
    public sealed class MissionPickupZone : MonoBehaviour
    {
        [Tooltip("Слой машины, способной активировать зону.")]
        [SerializeField] private LayerMask _vehicleMask;

        private IMissionService _missionService;
        private MissionScenePoint _point;

        private void Awake()
        {
            _point = GetComponent<MissionScenePoint>();
        }

        [Inject]
        public void Construct(IMissionService missionService)
        {
            _missionService = missionService;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_point == null || _point.Kind != MissionPointKind.Pickup || !IsVehicle(other))
            {
                return;
            }

            _missionService.TryPickup(_point.Id);
        }

        private bool IsVehicle(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            int layer = body == null ? other.gameObject.layer : body.gameObject.layer;
            return (_vehicleMask.value & (1 << layer)) != 0;
        }

        private void Reset()
        {
            _vehicleMask = LayerMask.GetMask("Vehicle");
            GetComponent<BoxCollider>().isTrigger = true;
        }
    }
}
