using UnityEngine;

namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    public sealed class SparkAnimationEvents : MonoBehaviour
    {
        public void CompleteEating()
        {
            SparkInteractable spark = GetComponentInParent<SparkInteractable>();

            if (spark != null)
            {
                spark.CompleteEating();
            }
        }
    }
}
