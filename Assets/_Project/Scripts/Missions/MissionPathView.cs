using ProjectSparks.Driving;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace ProjectSparks.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionPathView : MonoBehaviour
    {
        [Tooltip("Включить путеводные точки. При выключении скрывается цепочка и прекращаются лучи к Ground и обновление меша. Можно менять во время игры.")]
        [SerializeField] private bool _showDots = true;
        [Tooltip("Контроллер текущей цели этой сцены.")]
        [SerializeField] private MissionTargetView _targetView;
        [Tooltip("Общий материал светящихся круглых точек.")]
        [SerializeField] private Material _material;
        [Tooltip("Слои поверхности, на которой располагаются точки.")]
        [SerializeField] private LayerMask _groundMask = 1 << 6;
        [Tooltip("Подъем центра точек над поверхностью по ее нормали, в метрах.")]
        [SerializeField, Min(0f)] private float _groundOffset = 0.08f;
        [Tooltip("Минимальный отступ точек от центра машины в метрах.")]
        [SerializeField, Min(0f)] private float _vehicleOffset = 4f;
        [Tooltip("Отступ первой точки от центра цели в метрах. Шаг цепочки отсчитывается от цели.")]
        [SerializeField, Min(0f)] private float _targetOffset = 2.5f;
        [Tooltip("Расстояние между центрами точек в метрах.")]
        [SerializeField, Min(0.1f)] private float _spacing = 2.5f;
        [Tooltip("Диаметр точки вместе с мягким свечением, в метрах.")]
        [SerializeField, Min(0.01f)] private float _diameter = 0.45f;
        [Tooltip("Максимальное число точек. Емкость выделяется при первом включении точек в этой сцене.")]
        [SerializeField, Range(1, 512)] private int _maxDots = 96;
        [Tooltip("Высота начала луча над более высокой из двух точек маршрута, в метрах.")]
        [SerializeField, Min(0.1f)] private float _raycastHeight = 50f;
        [Tooltip("Длина луча вниз для поиска Ground, в метрах.")]
        [SerializeField, Min(1f)] private float _raycastDepth = 200f;

        private IVehicleTelemetry _vehicle;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private Transform _meshTransform;
        private Vector3[] _vertices;

        [Inject]
        private void Construct(IVehicleTelemetry vehicle) => _vehicle = vehicle;

        private void Start()
        {
            if (_targetView == null || _material == null || _vehicle == null)
            {
                Debug.LogError("В MissionPathView назначьте контроллер цели и материал; требуется машина из DrivingInstaller.", this);
                enabled = false;
                return;
            }

            if (_showDots)
                CreateMesh();
        }

        private void CreateMesh()
        {
            int capacity = Mathf.Clamp(_maxDots, 1, 512);
            _vertices = new Vector3[capacity * 4];
            var uv = new Vector2[capacity * 4];
            var triangles = new int[capacity * 6];
            for (int i = 0; i < capacity; i++)
            {
                int v = i * 4;
                uv[v] = Vector2.zero;
                uv[v + 1] = Vector2.right;
                uv[v + 2] = Vector2.one;
                uv[v + 3] = Vector2.up;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            var meshObject = new GameObject("MissionPathDots", typeof(MeshFilter), typeof(MeshRenderer));
            _meshTransform = meshObject.transform;
            _meshTransform.SetParent(transform, false);
            _mesh = new Mesh { name = "MissionPathDots" };
            _mesh.MarkDynamic();
            _mesh.vertices = _vertices;
            _mesh.uv = uv;
            _mesh.triangles = triangles;
            meshObject.GetComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = meshObject.GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderer.enabled = false;
        }

        private void LateUpdate()
        {
            if (!_showDots)
            {
                if (_renderer != null)
                    _renderer.enabled = false;
                return;
            }
            if (_mesh == null)
                CreateMesh();
            MissionScenePoint point = _targetView.CurrentPoint;
            if (point == null)
            {
                _renderer.enabled = false;
                return;
            }
            if (Time.timeScale == 0f)
                return;
            Rebuild(point.SurfacePosition);
        }

        private void Rebuild(Vector3 target)
        {
            Vector3 start = _vehicle.Position;
            Vector3 direction = start - target;
            direction.y = 0f;
            float distance = direction.magnitude;
            float first = Mathf.Max(0f, _targetOffset);
            float last = distance - Mathf.Max(0f, _vehicleOffset);
            if (distance < 0.001f || last < first)
            {
                _renderer.enabled = false;
                return;
            }
            direction /= distance;
            float spacing = Mathf.Max(0.1f, _spacing);
            int count = Mathf.Min(_vertices.Length / 4, Mathf.FloorToInt((last - first) / spacing) + 1);
            float rayY = Mathf.Max(start.y, target.y) + Mathf.Max(0.1f, _raycastHeight);
            int visible = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 origin = target + direction * (first + i * spacing);
                origin.y = rayY;
                if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                    Mathf.Max(1f, _raycastDepth), _groundMask, QueryTriggerInteraction.Ignore))
                    continue;
                Vector3 center = hit.point + hit.normal * Mathf.Max(0f, _groundOffset);
                float radius = Mathf.Max(0.01f, _diameter) * 0.5f;
                Vector3 right = Vector3.Cross(hit.normal, direction).normalized * radius;
                Vector3 forward = Vector3.Cross(right.normalized, hit.normal) * radius;
                int v = visible++ * 4;
                _vertices[v] = _meshTransform.InverseTransformPoint(center - right - forward);
                _vertices[v + 1] = _meshTransform.InverseTransformPoint(center + right - forward);
                _vertices[v + 2] = _meshTransform.InverseTransformPoint(center + right + forward);
                _vertices[v + 3] = _meshTransform.InverseTransformPoint(center - right + forward);
            }
            // Неиспользуемые четырехугольники вырождаются, буферы остаются прежними.
            for (int i = visible * 4; i < _vertices.Length; i++)
                _vertices[i] = Vector3.zero;
            _mesh.vertices = _vertices;
            _mesh.RecalculateBounds();
            _renderer.enabled = visible > 0;
        }

        private void OnDisable()
        {
            if (_renderer != null)
                _renderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (_meshTransform != null)
                Destroy(_meshTransform.gameObject);
            if (_mesh != null)
                Destroy(_mesh);
        }
    }
}
