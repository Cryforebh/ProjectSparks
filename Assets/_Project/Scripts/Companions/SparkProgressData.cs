using System;
using UnityEngine;

namespace ProjectSparks.Companions
{
    [Serializable]
    public sealed class SparkProgressData
    {
        [Tooltip("Тип Искры, которой принадлежат данные.")]
        [SerializeField] private CompanionType _companionType;
        [Tooltip("Пользовательское имя Искры.")]
        [SerializeField] private string _displayName;
        [Tooltip("Значение характеристики плавания.")]
        [SerializeField] private int _swim;
        [Tooltip("Значение характеристики полета.")]
        [SerializeField] private int _fly;
        [Tooltip("Значение характеристики бега.")]
        [SerializeField] private int _run;
        [Tooltip("Значение характеристики силы.")]
        [SerializeField] private int _power;
        [Tooltip("Значение характеристики выносливости.")]
        [SerializeField] private int _stamina;

        public SparkProgressData(
            CompanionType companionType,
            string displayName,
            int swim,
            int fly,
            int run,
            int power,
            int stamina)
        {
            _companionType = companionType;
            _displayName = displayName ?? string.Empty;
            _swim = Math.Max(0, swim);
            _fly = Math.Max(0, fly);
            _run = Math.Max(0, run);
            _power = Math.Max(0, power);
            _stamina = Math.Max(0, stamina);
        }

        public CompanionType CompanionType => _companionType;
        public string DisplayName => _displayName;

        public int GetStat(SparkStatType statType)
        {
            return statType switch
            {
                SparkStatType.Swim => _swim,
                SparkStatType.Fly => _fly,
                SparkStatType.Run => _run,
                SparkStatType.Power => _power,
                SparkStatType.Stamina => _stamina,
                _ => throw new ArgumentOutOfRangeException(nameof(statType))
            };
        }
    }
}
