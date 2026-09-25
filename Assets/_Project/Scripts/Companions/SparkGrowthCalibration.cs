using UnityEngine;

namespace ProjectSparks.Companions
{
    public abstract class SparkGrowthCalibration : ScriptableObject
    {
        public abstract SparkGrowthBinding Bind(Transform model);
    }

    public abstract class SparkGrowthBinding
    {
        public abstract void Apply(
            float growth,
            float earDevelopment,
            float earRest,
            float antennaBabyScale);
    }
}
