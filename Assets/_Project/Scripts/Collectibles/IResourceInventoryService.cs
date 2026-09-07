using System;

namespace ProjectSparks.Collectibles
{
    public interface IResourceInventoryService
    {
        event Action<ResourceType, int> AmountChanged;
        event Action<FruitType, int> FruitAmountChanged;

        int FruitCount { get; }
        int CapsuleCount { get; }

        int GetAmount(ResourceType resourceType);
        int GetFruitAmount(FruitType fruitType);
        void Add(ResourceType resourceType, int amount);
        void AddFruit(FruitType fruitType, int amount);
        bool TryRemoveFruit(FruitType fruitType, int amount);
        void Restore(int fruitCount, int capsuleCount);
        void RestoreFruits(
            int swimFruitCount,
            int flyFruitCount,
            int runFruitCount,
            int powerFruitCount,
            int staminaFruitCount);
        void Reset();
    }
}
