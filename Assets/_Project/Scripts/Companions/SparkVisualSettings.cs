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
        private readonly SparkBalanceSettings _balance;
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        private static readonly int _bodyColor = Shader.PropertyToID("_BodyColor");

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

                    if (material == null || !material.HasProperty(_bodyColor))
                    {
                        continue;
                    }

                    renderer.GetPropertyBlock(_properties, i);

                    _properties.SetColor(
                        _bodyColor,
                        spark.BodyColor);

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
