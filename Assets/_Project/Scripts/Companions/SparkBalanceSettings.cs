using System;
using ProjectSparks.Collectibles;
using UnityEngine;

namespace ProjectSparks.Companions
{
    [Serializable]
    public sealed class SparkAbilityGrowth
    {
        [Tooltip("Характеристика, открывающая и усиливающая способность у любой Искры.")]
        [SerializeField] private SparkStatType _stat;
        [Tooltip("Минимальное значение характеристики для открытия способности.")]
        [SerializeField, Min(0)] private int _unlockAt = 10;
        [Tooltip("Значение характеристики, при котором эффект достигает максимума. Должно быть больше порога открытия.")]
        [SerializeField, Min(1)] private int _maximumAt = 50;
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
        [Tooltip("Рост радиуса сбора ресурсов. Одинаковые правила для всех Искр.")]
        [SerializeField] private SparkAbilityGrowth _pickupRadius = new SparkAbilityGrowth(SparkStatType.Power, 0.5f, 1f);
        [Tooltip("Снижение расхода ускорения. Одинаковые правила для всех Искр.")]
        [SerializeField] private SparkAbilityGrowth _boostSaving = new SparkAbilityGrowth(SparkStatType.Stamina, 0.25f, 0.5f);
        [Tooltip("Прибавка к основной характеристике съеденного фрукта.")]
        [SerializeField, Min(0)] private int _primaryFruitIncrease = 5;
        [Tooltip("Прибавка к каждой из остальных характеристик от съеденного фрукта.")]
        [SerializeField, Min(0)] private int _secondaryFruitIncrease = 2;

        public SparkAbilityGrowth PickupRadius => _pickupRadius;
        public SparkAbilityGrowth BoostSaving => _boostSaving;

        public bool TryValidate(out string error)
        {
            if (_pickupRadius == null || !_pickupRadius.IsValid || _boostSaving == null || !_boostSaving.IsValid)
            {
                error = "Проверьте характеристики, пороги и эффекты: максимум характеристики должен быть выше порога, эффекты от 0 до 1 и не убывают.";
                return false;
            }
            if (_primaryFruitIncrease < 0 || _secondaryFruitIncrease < 0)
            {
                error = "Прибавки характеристик от фруктов не могут быть отрицательными.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        public SparkStatValues GetFruitIncrease(FruitType fruit)
        {
            if (!Enum.IsDefined(typeof(FruitType), fruit))
                throw new ArgumentOutOfRangeException(nameof(fruit));
            return new SparkStatValues(
                fruit == FruitType.Swim ? _primaryFruitIncrease : _secondaryFruitIncrease,
                fruit == FruitType.Fly ? _primaryFruitIncrease : _secondaryFruitIncrease,
                fruit == FruitType.Run ? _primaryFruitIncrease : _secondaryFruitIncrease,
                fruit == FruitType.Power ? _primaryFruitIncrease : _secondaryFruitIncrease,
                fruit == FruitType.Stamina ? _primaryFruitIncrease : _secondaryFruitIncrease);
        }

        private void OnValidate()
        {
            if (!TryValidate(out string error))
                Debug.LogError(error, this);
        }
    }
}
