using System;

namespace ProjectSparks.Core
{
    public sealed class EconomyService : IEconomyService
    {
        public event Action<int> BalanceChanged;

        public int Balance { get; private set; }

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Balance += amount;
            BalanceChanged?.Invoke(Balance);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > Balance)
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            Balance -= amount;
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public void Restore(int balance)
        {
            int restoredBalance = Math.Max(0, balance);
            if (Balance == restoredBalance)
            {
                return;
            }

            Balance = restoredBalance;
            BalanceChanged?.Invoke(Balance);
        }

        public void Reset()
        {
            Restore(0);
        }
    }
}
