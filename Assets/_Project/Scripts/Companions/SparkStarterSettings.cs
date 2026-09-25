using UnityEngine;

namespace ProjectSparks.Companions
{
    [CreateAssetMenu(
        fileName = "SparkStarterSettings",
        menuName = "Project Sparks/Spark Starter Settings")]
    public sealed class SparkStarterSettings : ScriptableObject
    {
        [Tooltip("Искры, которые игрок имеет в начале новой игры.")]
        [SerializeField] private SparkDefinition[] _definitions;

        public SparkDefinition[] Definitions => _definitions;
    }
}
