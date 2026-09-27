using UnityEngine;

namespace ProjectSparks.Companions
{
    [CreateAssetMenu(
        fileName = "SparkDefinition",
        menuName = "Project Sparks/Spark Definition")]
    public sealed class SparkDefinition : ScriptableObject
    {
        [Header("Основное")]

        [Tooltip("Постоянный уникальный ID Искры.")]
        [SerializeField] private string _id;

        [Tooltip("Имя Искры при ее первом создании.")]
        [SerializeField] private string _defaultName = "Искра";

        [Header("Внешность")]

        [Tooltip("Основной индивидуальный цвет Искры.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color _bodyColor = SparkColorDefaults.Body;

        [ColorUsage(true, true)]
        [SerializeField] private Color _secondaryColor = SparkColorDefaults.Secondary;

        [ColorUsage(true, true)]
        [SerializeField] private Color _accentColor = SparkColorDefaults.Accent;

        [Tooltip("Постоянное базовое выражение мордочки Искры.")]
        [SerializeField] private SparkEmotion _emotion = SparkEmotion.Neutral;

        [Header("Стартовые характеристики")]

        [MinTooltip(0, "Стартовое значение плавания.")]
        [SerializeField] private int _swim;

        [MinTooltip(0, "Стартовое значение полета.")]
        [SerializeField] private int _fly;

        [MinTooltip(0, "Стартовое значение бега.")]
        [SerializeField] private int _run;

        [MinTooltip(0, "Стартовое значение силы.")]
        [SerializeField] private int _power;

        [MinTooltip(0, "Стартовое значение выносливости.")]
        [SerializeField] private int _stamina;

        public string Id => _id;
        public string DefaultName => _defaultName;
        public Color BodyColor => _bodyColor;
        public Color SecondaryColor => _secondaryColor;
        public Color AccentColor => _accentColor;
        public SparkEmotion Emotion => _emotion;

        public SparkStatValues InitialStats =>
            new SparkStatValues(
                _swim,
                _fly,
                _run,
                _power,
                _stamina);

        private void OnValidate()
        {
            if (_id != null)
            {
                _id = _id.Trim();
            }

            if (_defaultName != null)
            {
                _defaultName = _defaultName.Trim();
            }
        }
    }
}
