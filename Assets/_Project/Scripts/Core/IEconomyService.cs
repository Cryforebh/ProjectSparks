using System;

namespace ProjectSparks.Core
{
    public interface IEconomyService
    {
        event Action<int> BalanceChanged;

        int Balance { get; }

        void Add(int amount);
        bool TrySpend(int amount);
        void Restore(int balance);
        void Reset();
    }
}
