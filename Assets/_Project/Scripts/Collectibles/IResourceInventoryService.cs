using System;
using System.Collections.Generic;

namespace ProjectSparks.Collectibles
{
    public interface IResourceInventoryService
    {
        event Action<ResourceType, int> AmountChanged;
        event Action<string, int> FruitAmountChanged;

        int FruitCount { get; }
        int CapsuleCount { get; }

        int GetAmount(ResourceType resourceType);
        IReadOnlyDictionary<string, int> FruitAmounts { get; }
        int GetFruitAmount(string fruitId);
        void Add(ResourceType resourceType, int amount);
        void AddFruit(string fruitId, int amount);
        bool TryRemoveFruit(string fruitId, int amount);
        void RestoreCapsules(int capsuleCount);
        void RestoreFruits(IReadOnlyDictionary<string, int> amounts);
        void Reset();
    }
}
