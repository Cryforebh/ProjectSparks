using UnityEngine;
namespace ProjectSparks.Driving
{
    [DisallowMultipleComponent]
    public sealed class PassengerSeat : MonoBehaviour
    {
        [Tooltip("Точка размещения пассажира. Машина предоставляет только место, без данных о пассажире.")]
        [SerializeField] private Transform _point;
        public Transform Point => _point;
    }
}
