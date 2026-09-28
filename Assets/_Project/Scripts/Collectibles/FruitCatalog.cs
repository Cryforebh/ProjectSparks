using System;
using System.Collections.Generic;
using ProjectSparks.Companions;
using UnityEngine;

namespace ProjectSparks.Collectibles
{
    [Serializable]
    public sealed class FruitAmountData
    {
        [SerializeField] private string _fruitId;
        [SerializeField] private int _amount;

        public FruitAmountData(string fruitId, int amount)
        {
            _fruitId = fruitId;
            _amount = amount;
        }

        public string FruitId => _fruitId;
        public int Amount => _amount;
    }

    [CreateAssetMenu(fileName = "FruitCatalog", menuName = "Project Sparks/Fruits/Fruit Catalog")]
    public sealed class FruitCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Физический префаб. ID и название берутся из PhysicalFruitItem.")]
            [SerializeField] private PhysicalFruitItem _prefab;
            [Tooltip("Иконка фрукта в интерфейсе. Пока для всех фруктов иконки не подготовлены.")]
            [SerializeField] private Sprite _icon;
            [Min(0), Tooltip("Фруктов в сундуке при новой игре или сбросе прогресса.")]
            [SerializeField] private int _startingAmount;

            [Header("Прибавки от кормления")]
            [SerializeField, Min(0)] private int _swimIncrease;
            [SerializeField, Min(0)] private int _flyIncrease;
            [SerializeField, Min(0)] private int _runIncrease;
            [SerializeField, Min(0)] private int _powerIncrease;
            [SerializeField, Min(0)] private int _staminaIncrease;

            public PhysicalFruitItem Prefab => _prefab;
            public string FruitId => _prefab != null ? _prefab.FruitId : null;
            public Sprite Icon => _icon;
            public int StartingAmount => _startingAmount;
            public SparkStatValues StatIncrease => new SparkStatValues(
                _swimIncrease, _flyIncrease, _runIncrease, _powerIncrease, _staminaIncrease);

            internal bool HasValidAmounts => _startingAmount >= 0 &&
                _swimIncrease >= 0 && _flyIncrease >= 0 && _runIncrease >= 0 &&
                _powerIncrease >= 0 && _staminaIncrease >= 0;
        }

        [Tooltip("Фрукты в порядке отображения в интерфейсе. Новые записи добавляются вручную.")]
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public int Count => _entries?.Length ?? 0;
        public Entry GetEntry(int index) => _entries[index];
        public bool Contains(string fruitId) =>
            !string.IsNullOrWhiteSpace(fruitId) && TryGetEntry(fruitId, out _);

        public bool TryGetEntry(string fruitId, out Entry entry)
        {
            for (int i = 0; i < Count; i++)
            {
                Entry candidate = _entries[i];
                if (candidate != null &&
                    string.Equals(candidate.FruitId, fruitId, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public bool TryValidate(out string error)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Count; i++)
            {
                Entry entry = _entries[i];
                if (entry?.Prefab == null)
                {
                    error = $"Запись фрукта {i + 1}: не назначен физический префаб.";
                    return false;
                }

                string id = entry.FruitId;
                if (string.IsNullOrWhiteSpace(id) || id != id.Trim())
                {
                    error = $"Запись фрукта {i + 1}: в префабе нужен непустой ID без пробелов по краям.";
                    return false;
                }

                if (!ids.Add(id))
                {
                    error = $"Повторяется ID фрукта '{id}'.";
                    return false;
                }

                if (!entry.HasValidAmounts)
                {
                    error = $"Запись фрукта {i + 1}: количества и прибавки не могут быть отрицательными.";
                    return false;
                }
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
