using UnityEngine;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class SceneSpawnPoint : MonoBehaviour
    {
        [Tooltip("Уникальный идентификатор точки появления в этой сцене.")]
        [SerializeField] private string _id;

        public string Id => _id;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            Gizmos.DrawLine(
                transform.position,
                transform.position + transform.forward * 0.8f);
        }
    }
}
