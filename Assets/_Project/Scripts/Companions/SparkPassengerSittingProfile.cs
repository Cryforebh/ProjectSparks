using UnityEngine;

namespace ProjectSparks.Companions
{
    [CreateAssetMenu(fileName = "SparkPassengerSittingProfile", menuName = "Project Sparks/Spark Passenger Sitting Profile")]
    public sealed class SparkPassengerSittingProfile : ScriptableObject
    {
        [Tooltip("Спокойное сидение пассажира. После него всегда проигрывается игривое сидение.")]
        [SerializeField] private SparkSittingProfile.Variant _resting;
        [Tooltip("Игривое сидение пассажира. После него всегда возвращается спокойное сидение.")]
        [SerializeField] private SparkSittingProfile.Variant _playful;

        public SparkSittingProfile.Variant Resting => _resting;
        public SparkSittingProfile.Variant Playful => _playful;
    }
}
