using UnityEngine;

namespace ProjectSparks.Missions
{
    public enum MissionPointKind
    {
        Pickup = 0,
        Delivery = 1
    }

    [DisallowMultipleComponent]
    public sealed class MissionScenePoint : MonoBehaviour
    {
        [Tooltip("Уникальный идентификатор точки внутри этой сцены.")]
        [SerializeField] private string _id;
        [Tooltip("Название склада или места доставки для игрока.")]
        [SerializeField] private string _label;
        [Tooltip("Назначение точки: получение груза или доставка.")]
        [SerializeField] private MissionPointKind _kind;

        public string Id => _id;
        public string Label => _label;
        public MissionPointKind Kind => _kind;

        public Vector3 SurfacePosition
        {
            get
            {
                BoxCollider zone = GetComponent<BoxCollider>();
                if (zone == null)
                    return transform.position;
                Bounds bounds = zone.bounds;
                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Camera.current == null || Camera.current.cameraType != CameraType.SceneView)
                return;
            BoxCollider zone = GetComponent<BoxCollider>();
            if (zone == null)
                return;

            Color previousColor = Gizmos.color;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color color = _kind == MissionPointKind.Pickup
                ? new Color(0.1f, 0.85f, 1f, 1f)
                : new Color(1f, 0.65f, 0.1f, 1f);

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(color.r, color.g, color.b, 0.08f);
            Gizmos.DrawCube(zone.center, zone.size);
            Gizmos.color = color;
            Gizmos.DrawWireCube(zone.center, zone.size);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;

            string kind = _kind == MissionPointKind.Pickup ? "Склад" : "Доставка";
            UnityEditor.Handles.Label(SurfacePosition + Vector3.up * 0.3f, $"{kind}: {_label} [{_id}]");
        }
#endif
    }
}
