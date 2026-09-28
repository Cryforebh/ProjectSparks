using System;
using System.Collections.Generic;
using ProjectSparks.Collectibles;
using ProjectSparks.Companions;
using UnityEngine;

namespace ProjectSparks.Infrastructure
{
    [Serializable]
    public sealed class GameProgressData
    {
        public const int CurrentVersion = 2;

        [SerializeField] private int _version;
        [SerializeField] private int _balance;
        [SerializeField] private int _capsuleCount;
        [SerializeField] private FruitAmountData[] _fruits;
        [SerializeField] private string _lastCommittedTripId;
        [SerializeField] private string _selectedSparkId;
        [SerializeField] private SparkProgressData[] _sparks;

        public GameProgressData(int balance, string selectedCompanion, int capsuleCount,
            IReadOnlyDictionary<string, int> fruits = null, string lastCommittedTripId = null,
            SparkProgressData[] sparks = null)
        {
            _version = CurrentVersion;
            _balance = Math.Max(0, balance);
            _selectedSparkId = selectedCompanion ?? string.Empty;
            _capsuleCount = Math.Max(0, capsuleCount);
            _lastCommittedTripId = lastCommittedTripId;
            _sparks = sparks;

            List<FruitAmountData> entries = new List<FruitAmountData>();
            if (fruits != null)
            {
                foreach (KeyValuePair<string, int> pair in fruits)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0) continue;
                    entries.Add(new FruitAmountData(pair.Key, pair.Value));
                }
            }
            entries.Sort((a, b) => StringComparer.Ordinal.Compare(a.FruitId, b.FruitId));
            _fruits = entries.ToArray();
        }

        public int Version => _version;
        public int Balance => _balance;
        public string SelectedCompanion => _selectedSparkId ?? string.Empty;
        public SparkProgressData[] Sparks => _sparks;
        public int CapsuleCount => _capsuleCount;
        public string LastCommittedTripId => _lastCommittedTripId;

        public IReadOnlyDictionary<string, int> GetFruitAmounts(FruitCatalog catalog)
        {
            Dictionary<string, int> amounts = new Dictionary<string, int>(StringComparer.Ordinal);
            if (_fruits == null) return amounts;
            foreach (FruitAmountData entry in _fruits)
            {
                if (entry == null || entry.Amount <= 0 || !catalog.Contains(entry.FruitId))
                    continue;
                amounts.TryGetValue(entry.FruitId, out int current);
                amounts[entry.FruitId] = checked(current + entry.Amount);
            }
            return amounts;
        }
    }
}
