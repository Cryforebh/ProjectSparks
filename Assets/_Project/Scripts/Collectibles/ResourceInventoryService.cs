using System;
using System.Collections.Generic;

namespace ProjectSparks.Collectibles
{
    public sealed class ResourceInventoryService : IResourceInventoryService
    {
        private readonly FruitCatalog _catalog;
        private readonly Dictionary<string, int> _fruitAmounts = new Dictionary<string, int>(StringComparer.Ordinal);

        public ResourceInventoryService(FruitCatalog catalog)
        {
            _catalog = catalog;
        }

        public event Action<ResourceType, int> AmountChanged;
        public event Action<string, int> FruitAmountChanged;

        public int FruitCount { get; private set; }
        public int CapsuleCount { get; private set; }
        public IReadOnlyDictionary<string, int> FruitAmounts => _fruitAmounts;

        public int GetAmount(ResourceType resourceType) => resourceType switch
        {
            ResourceType.Fruit => FruitCount,
            ResourceType.Capsule => CapsuleCount,
            _ => throw new ArgumentOutOfRangeException(nameof(resourceType))
        };

        public int GetFruitAmount(string fruitId)
        {
            ValidateFruitId(fruitId);
            return _fruitAmounts.TryGetValue(fruitId, out int amount) ? amount : 0;
        }

        public void Add(ResourceType resourceType, int amount)
        {
            if (amount <= 0) return;
            if (resourceType == ResourceType.Fruit)
                throw new InvalidOperationException("Для добавления фрукта нужен его ID.");
            if (resourceType != ResourceType.Capsule)
                throw new ArgumentOutOfRangeException(nameof(resourceType));
            CapsuleCount = checked(CapsuleCount + amount);
            AmountChanged?.Invoke(resourceType, CapsuleCount);
        }

        public void AddFruit(string fruitId, int amount)
        {
            ValidateFruitId(fruitId);
            if (amount <= 0) return;
            int updated = checked(GetFruitAmount(fruitId) + amount);
            int total = checked(FruitCount + amount);
            _fruitAmounts[fruitId] = updated;
            FruitCount = total;
            FruitAmountChanged?.Invoke(fruitId, updated);
            AmountChanged?.Invoke(ResourceType.Fruit, FruitCount);
        }

        public bool TryRemoveFruit(string fruitId, int amount)
        {
            ValidateFruitId(fruitId);
            if (amount <= 0 || GetFruitAmount(fruitId) < amount) return false;
            int updated = _fruitAmounts[fruitId] - amount;
            if (updated == 0) _fruitAmounts.Remove(fruitId);
            else _fruitAmounts[fruitId] = updated;
            FruitCount -= amount;
            FruitAmountChanged?.Invoke(fruitId, updated);
            AmountChanged?.Invoke(ResourceType.Fruit, FruitCount);
            return true;
        }

        public void RestoreCapsules(int capsuleCount)
        {
            int restored = Math.Max(0, capsuleCount);
            if (CapsuleCount == restored) return;
            CapsuleCount = restored;
            AmountChanged?.Invoke(ResourceType.Capsule, CapsuleCount);
        }

        public void RestoreFruits(IReadOnlyDictionary<string, int> amounts)
        {
            Dictionary<string, int> restored = new Dictionary<string, int>(StringComparer.Ordinal);
            int total = 0;
            if (amounts != null)
            {
                foreach (KeyValuePair<string, int> pair in amounts)
                {
                    ValidateFruitId(pair.Key);
                    if (pair.Value < 0) throw new ArgumentOutOfRangeException(nameof(amounts));
                    if (pair.Value == 0) continue;
                    restored.Add(pair.Key, pair.Value);
                    total = checked(total + pair.Value);
                }
            }

            HashSet<string> changed = new HashSet<string>(_fruitAmounts.Keys, StringComparer.Ordinal);
            changed.UnionWith(restored.Keys);
            Dictionary<string, int> previous = new Dictionary<string, int>(_fruitAmounts, StringComparer.Ordinal);
            _fruitAmounts.Clear();
            foreach (KeyValuePair<string, int> pair in restored) _fruitAmounts.Add(pair.Key, pair.Value);
            int previousTotal = FruitCount;
            FruitCount = total;
            foreach (string id in changed)
            {
                previous.TryGetValue(id, out int before);
                restored.TryGetValue(id, out int after);
                if (before != after) FruitAmountChanged?.Invoke(id, after);
            }
            if (previousTotal != total) AmountChanged?.Invoke(ResourceType.Fruit, total);
        }

        public void Reset()
        {
            RestoreFruits(null);
            RestoreCapsules(0);
        }

        private void ValidateFruitId(string fruitId)
        {
            if (!_catalog.Contains(fruitId))
                throw new ArgumentException("Фрукт отсутствует в каталоге.", nameof(fruitId));
        }
    }
}
