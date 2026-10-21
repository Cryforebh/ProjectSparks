using ProjectSparks.Companions;
using UnityEngine;
namespace ProjectSparks.Driving
{
    [DisallowMultipleComponent]
    public sealed class PassengerSeat : MonoBehaviour
    {
        [Tooltip("Точка размещения пассажира. Машина предоставляет только место, без данных о пассажире.")]
        [SerializeField] private Transform _point;
        [Tooltip("Профиль чередования спокойного и игривого сидения пассажира в кузове.")]
        [SerializeField] private SparkPassengerSittingProfile _sittingProfile;
        public Transform Point => _point;
        public SparkPassengerSittingProfile SittingProfile => _sittingProfile;
    }
}
