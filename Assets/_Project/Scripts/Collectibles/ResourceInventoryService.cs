using System;

namespace ProjectSparks.Collectibles
{
    public sealed class ResourceInventoryService : IResourceInventoryService
    {
        private readonly int[] _fruitAmounts = new int[5];

        public event Action<ResourceType, int> AmountChanged;
        public event Action<FruitType, int> FruitAmountChanged;

        public int FruitCount { get; private set; }
        public int CapsuleCount { get; private set; }

        public int GetAmount(ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.Fruit => FruitCount,
                ResourceType.Capsule => CapsuleCount,
                _ => throw new ArgumentOutOfRangeException(nameof(resourceType))
            };
        }

        public int GetFruitAmount(FruitType fruitType)
        {
            return _fruitAmounts[GetFruitIndex(fruitType)];
        }

        public void Add(ResourceType resourceType, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            switch (resourceType)
            {
                case ResourceType.Fruit:
                    AddFruit(FruitType.Fly, amount);
                    break;

                case ResourceType.Capsule:
                    CapsuleCount += amount;
                    AmountChanged?.Invoke(resourceType, CapsuleCount);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(resourceType));
            }
        }

        public void AddFruit(FruitType fruitType, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int index = GetFruitIndex(fruitType);
            _fruitAmounts[index] = checked(_fruitAmounts[index] + amount);
            FruitCount = checked(FruitCount + amount);
            FruitAmountChanged?.Invoke(fruitType, _fruitAmounts[index]);
            AmountChanged?.Invoke(ResourceType.Fruit, FruitCount);
        }

        public bool TryRemoveFruit(FruitType fruitType, int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            int index = GetFruitIndex(fruitType);
            if (_fruitAmounts[index] < amount || FruitCount < amount)
            {
                return false;
            }

            _fruitAmounts[index] -= amount;
            FruitCount -= amount;
            FruitAmountChanged?.Invoke(fruitType, _fruitAmounts[index]);
            AmountChanged?.Invoke(ResourceType.Fruit, FruitCount);
            return true;
        }

        public void Restore(int fruitCount, int capsuleCount)
        {
            int restoredFruitCount = Math.Max(0, fruitCount);
            int restoredCapsuleCount = Math.Max(0, capsuleCount);

            if (FruitCount != restoredFruitCount)
            {
                FruitCount = restoredFruitCount;
                AmountChanged?.Invoke(ResourceType.Fruit, FruitCount);
            }

            if (CapsuleCount != restoredCapsuleCount)
            {
                CapsuleCount = restoredCapsuleCount;
                AmountChanged?.Invoke(ResourceType.Capsule, CapsuleCount);
            }
        }

        public void RestoreFruits(
            int swimFruitCount,
            int flyFruitCount,
            int runFruitCount,
            int powerFruitCount,
            int staminaFruitCount)
        {
            int[] restoredAmounts =
            {
                Math.Max(0, swimFruitCount),
                Math.Max(0, flyFruitCount),
                Math.Max(0, runFruitCount),
                Math.Max(0, powerFruitCount),
                Math.Max(0, staminaFruitCount)
            };

            int restoredTotal = 0;
            for (int index = 0; index < restoredAmounts.Length; index++)
            {
                restoredTotal = checked(restoredTotal + restoredAmounts[index]);
            }

            if (restoredTotal == 0 && FruitCount > 0)
            {
                restoredAmounts[(int)FruitType.Fly] = FruitCount;
                restoredTotal = FruitCount;
            }

            for (int index = 0; index < _fruitAmounts.Length; index++)
            {
                if (_fruitAmounts[index] == restoredAmounts[index])
                {
                    continue;
                }

                _fruitAmounts[index] = restoredAmounts[index];
                FruitAmountChanged?.Invoke(
                    (FruitType)index,
                    _fruitAmounts[index]);
            }

            if (FruitCount != restoredTotal)
            {
                FruitCount = restoredTotal;
                AmountChanged?.Invoke(ResourceType.Fruit, FruitCount);
            }
        }

        public void Reset()
        {
            for (int index = 0; index < _fruitAmounts.Length; index++)
            {
                if (_fruitAmounts[index] == 0)
                {
                    continue;
                }

                _fruitAmounts[index] = 0;
                FruitAmountChanged?.Invoke((FruitType)index, 0);
            }

            Restore(0, 0);
        }

        private static int GetFruitIndex(FruitType fruitType)
        {
            int index = (int)fruitType;
            if (index < 0 || index >= 5)
            {
                throw new ArgumentOutOfRangeException(nameof(fruitType));
            }

            return index;
        }
    }
}
