using UnityEngine;
namespace ProjectSparks.Companions
{
    [CreateAssetMenu(fileName = "SparkVisualSettings", menuName = "Project Sparks/Spark Visual Settings")]
    public sealed class SparkVisualSettings : ScriptableObject
    {
        [Tooltip("Общая визуальная модель для убежища и поездки. Без коллайдеров и логики взаимодействия.")]
        [SerializeField] private GameObject _basePrefab;
        public GameObject BasePrefab => _basePrefab;

        [Tooltip("Материал чешуи и кончиков усиков, окрашиваемый первым цветом Искры.")]
        [SerializeField] private Material _scalesMaterial;
        public Material ScalesMaterial => _scalesMaterial;
    }

    public sealed class SparkVisualFactory
    {
        private readonly SparkVisualSettings _settings;
        private readonly SparkBalanceSettings _balance;
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        private static readonly int _bodyColor = Shader.PropertyToID("_BodyColor");
        private static readonly int _secondaryColor = Shader.PropertyToID("_SecondaryColor");
        private static readonly int _accentColor = Shader.PropertyToID("_AccentColor");
        private static readonly int _baseColor = Shader.PropertyToID("_BaseColor");

        public SparkVisualFactory(SparkVisualSettings settings, SparkBalanceSettings balance)
        {
            _settings = settings;
            _balance = balance;
        }

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
            // Цвет
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];

                    if (material == null)
                    {
                        continue;
                    }

                    if (material == _settings.ScalesMaterial)
                    {
                        renderer.GetPropertyBlock(_properties, i);
                        _properties.SetColor(_baseColor, spark.BodyColor);
                        renderer.SetPropertyBlock(_properties, i);
                        _properties.Clear();
                        continue;
                    }

                    if (!material.HasProperty(_bodyColor))
                    {
                        continue;
                    }

                    renderer.GetPropertyBlock(_properties, i);

                    _properties.SetColor(_bodyColor, spark.BodyColor);

                    if (material.HasProperty(_secondaryColor))
                    {
                        _properties.SetColor(_secondaryColor, spark.SecondaryColor);
                    }

                    if (material.HasProperty(_accentColor))
                    {
                        _properties.SetColor(_accentColor, spark.AccentColor);
                    }

                    renderer.SetPropertyBlock(
                        _properties,
                        i);

                    _properties.Clear();
                }
            }

            // Постоянная эмоция
            SparkEmotionView emotionView = instance.GetComponent<SparkEmotionView>();

            if (emotionView != null)
            {
                emotionView.SetEmotion(spark.Emotion);
            }

            // Рост и уши
            SparkGrowthView growthView = instance.GetComponent<SparkGrowthView>();

            if (growthView != null)
            {
                growthView.SetDevelopment(spark.Growth, _balance.GetEarDevelopment(spark.Stats));
            }
        }
    }
}
