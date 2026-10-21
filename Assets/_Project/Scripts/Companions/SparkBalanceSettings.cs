using System;
using UnityEngine;

namespace ProjectSparks.Companions
{
    [Serializable]
    public sealed class SparkAbilityGrowth
    {
        [Tooltip("Характеристика, открывающая и усиливающая способность у любой Искры.")]
        [SerializeField] private SparkStatType _stat;
        [Min(0), Tooltip("Минимальное значение характеристики для открытия способности.")]
        [SerializeField] private int _unlockAt = 10;
        [Min(1), Tooltip("Значение характеристики, при котором эффект достигает максимума. Должно быть больше порога открытия.")]
        [SerializeField] private int _maximumAt = 50;
        [Tooltip("Доля эффекта при открытии: 0.5 означает 50 процентов.")]
        [SerializeField, Range(0f, 1f)] private float _initialBonus;
        [Tooltip("Максимальная доля эффекта: 1 означает 100 процентов. Не меньше начального эффекта.")]
        [SerializeField, Range(0f, 1f)] private float _maximumBonus;

        public SparkAbilityGrowth(SparkStatType stat, float initialBonus, float maximumBonus)
        {
            _stat = stat;
            _initialBonus = initialBonus;
            _maximumBonus = maximumBonus;
        }

        public SparkStatType Stat => _stat;
        public int UnlockAt => _unlockAt;
        public int MaximumAt => _maximumAt;

        public float Evaluate(SparkStatValues stats)
        {
            int value = stats.GetValue(_stat);
            if (value < _unlockAt)
                return 0f;
            float progress = Mathf.Clamp01((float)(((double)value - _unlockAt) / Math.Max(1d, (double)_maximumAt - _unlockAt)));
            return Mathf.Lerp(_initialBonus, _maximumBonus, progress);
        }

        public bool IsValid => Enum.IsDefined(typeof(SparkStatType), _stat) &&
            _unlockAt >= 0 && _maximumAt > _unlockAt &&
            !float.IsNaN(_initialBonus) && !float.IsNaN(_maximumBonus) &&
            _initialBonus >= 0f && _maximumBonus >= _initialBonus && _maximumBonus <= 1f;
    }

    [CreateAssetMenu(fileName = "SparkBalanceSettings", menuName = "Project Sparks/Spark Balance Settings")]
    public sealed class SparkBalanceSettings : ScriptableObject
    {
        [Tooltip("Рост радиуса сбора ресурсов. Одинаковые правила для всех Искр."), Space]
        [SerializeField] private SparkAbilityGrowth _pickupRadius = new SparkAbilityGrowth(SparkStatType.Power, 0.5f, 1f);
        [Tooltip("Снижение расхода ускорения. Одинаковые правила для всех Искр."), Space]
        [SerializeField] private SparkAbilityGrowth _boostSaving = new SparkAbilityGrowth(SparkStatType.Stamina, 0.25f, 0.5f);
        [Min(1), Tooltip("Количество успешных кормлений от маленькой Искры до полностью взрослой."), Space]
        [SerializeField] private int _feedingsToFullGrowth = 10;
        [Min(1), Tooltip("Значение характеристики полета, при котором уши Искры достигают максимального развития."), Space]
        [SerializeField] private int _earDevelopmentMaximumAt = 100;

        public int FeedingsToFullGrowth => _feedingsToFullGrowth;
        public float GrowthPerFeeding => 1f / Mathf.Max(1, _feedingsToFullGrowth);
        public int EarDevelopmentMaximumAt => _earDevelopmentMaximumAt;

        public SparkAbilityGrowth PickupRadius => _pickupRadius;
        public SparkAbilityGrowth BoostSaving => _boostSaving;

        public float GetEarDevelopment(SparkStatValues stats)
        {
            return Mathf.Clamp01(
                (float)stats.GetValue(SparkStatType.Fly)
                / Mathf.Max(1, _earDevelopmentMaximumAt));
        }

        public bool TryValidate(out string error)
        {
            if (_pickupRadius == null || !_pickupRadius.IsValid || _boostSaving == null || !_boostSaving.IsValid)
            {
                error = "Проверьте характеристики, пороги и эффекты: максимум характеристики должен быть выше порога, эффекты от 0 до 1 и не убывают.";
                return false;
            }
            if (_feedingsToFullGrowth < 1)
            {
                error = "Количество кормлений до полного взросления должно быть не меньше 1.";
                return false;
            }
            if (_earDevelopmentMaximumAt < 1)
            {
                error = "Порог полного развития ушей должен быть не меньше 1.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void OnValidate()
        {
            if (!TryValidate(out string error))
                Debug.LogError(error, this);
        }
    }
}
