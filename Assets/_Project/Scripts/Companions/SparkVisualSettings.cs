using UnityEngine;
namespace ProjectSparks.Companions
{
    [CreateAssetMenu(fileName = "SparkVisualSettings", menuName = "Project Sparks/Spark Visual Settings")]
    public sealed class SparkVisualSettings : ScriptableObject
    {
        [Tooltip("Общая визуальная модель для убежища и поездки. Без коллайдеров и логики взаимодействия.")]
        [SerializeField] private GameObject _basePrefab;
        public GameObject BasePrefab => _basePrefab;
    }

    public sealed class SparkVisualFactory
    {
        private readonly SparkVisualSettings _settings;
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        private static readonly int _baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int _color = Shader.PropertyToID("_Color");
        public SparkVisualFactory(SparkVisualSettings settings) { _settings = settings; }
        public GameObject Create(SparkProgressData spark, Transform parent)
        {
            var instance = Object.Instantiate(_settings.BasePrefab, parent, false);
            instance.name = "SparkVisual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            Apply(instance, spark);
            return instance;
        }
        public void Apply(GameObject instance, SparkProgressData spark)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.GetPropertyBlock(_properties);
                _properties.SetColor(_baseColor, spark.BodyColor);
                _properties.SetColor(_color, spark.BodyColor);
                renderer.SetPropertyBlock(_properties);
                _properties.Clear();
            }
        }
    }
}
