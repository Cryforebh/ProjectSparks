using UnityEngine;
using Zenject;

namespace ProjectSparks.Missions
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MissionDeliveryZone : MonoBehaviour
    {
        [Tooltip("Миссия, груз которой принимается в этой зоне.")]
        [SerializeField] private MissionDefinition _mission;
        [Tooltip("Слой машины, способной активировать зону.")]
        [SerializeField] private LayerMask _vehicleMask;

        private IMissionService _missionService;

        [Inject]
        public void Construct(IMissionService missionService)
        {
            _missionService = missionService;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_mission == null || !IsVehicle(other))
            {
                return;
            }

            _missionService.TryComplete(_mission);
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
