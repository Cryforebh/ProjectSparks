using System;
using System.Collections.Generic;
using ProjectSparks.Collectibles;

namespace ProjectSparks.Missions
{
    public sealed class TripSessionService : ITripSessionService
    {
        private readonly Dictionary<string, int> _fruitAmounts = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly FruitCatalog _catalog;
        private readonly HashSet<string> _completedDeliveryIds = new HashSet<string>(StringComparer.Ordinal);

        public TripSessionService(FruitCatalog catalog) => _catalog = catalog;

        public event Action Changed;

        public string LocationId { get; private set; }
        public string SessionId { get; private set; }
        public string ActiveDeliveryId { get; private set; }
        public TripSessionState State { get; private set; }
        public int RequiredDeliveryCount { get; private set; }
        public int CompletedDeliveryCount => _completedDeliveryIds.Count;
        public int Coins { get; private set; }
        public int FruitCount { get; private set; }
        public int CapsuleCount { get; private set; }
        public IReadOnlyDictionary<string, int> FruitAmounts => _fruitAmounts;

        private bool CanCollect => State == TripSessionState.InProgress || State == TripSessionState.Succeeded;

        public int GetFruitAmount(string fruitId)
        {
            if (!_catalog.Contains(fruitId))
                throw new ArgumentException("Фрукт отсутствует в каталоге.", nameof(fruitId));
            return _fruitAmounts.TryGetValue(fruitId, out int amount) ? amount : 0;
        }

        public bool TryBegin(string locationId, int requiredDeliveryCount)
        {
            if (string.IsNullOrWhiteSpace(locationId) || requiredDeliveryCount <= 0 || CanCollect)
            {
                return false;
            }

            ClearResult();
            LocationId = locationId;
            SessionId = Guid.NewGuid().ToString("N");
            RequiredDeliveryCount = requiredDeliveryCount;
            State = TripSessionState.InProgress;
            Changed?.Invoke();
            return true;
        }

        public bool TryBeginDelivery(string deliveryId)
        {
            if (State != TripSessionState.InProgress || ActiveDeliveryId != null ||
                string.IsNullOrWhiteSpace(deliveryId) || _completedDeliveryIds.Contains(deliveryId))
            {
                return false;
            }

            ActiveDeliveryId = deliveryId;
            Changed?.Invoke();
            return true;
        }

        public bool TryCompleteDelivery(string deliveryId, int coinReward)
        {
            if (State != TripSessionState.InProgress || ActiveDeliveryId == null ||
                !string.Equals(ActiveDeliveryId, deliveryId, StringComparison.Ordinal) ||
                coinReward < 0 || coinReward > int.MaxValue - Coins)
            {
                return false;
            }

            _completedDeliveryIds.Add(deliveryId);
            ActiveDeliveryId = null;
            Coins += coinReward;
            if (CompletedDeliveryCount == RequiredDeliveryCount)
            {
                State = TripSessionState.Succeeded;
            }

            Changed?.Invoke();
            return true;
        }

        public bool TryAddCoins(int amount)
        {
            if (!CanCollect || amount <= 0 || amount > int.MaxValue - Coins)
            {
                return false;
            }

            Coins += amount;
            Changed?.Invoke();
            return true;
        }

        public bool TryAddFruit(string fruitId, int amount)
        {
            if (!CanCollect || !_catalog.Contains(fruitId) ||
                amount <= 0 || amount > int.MaxValue - FruitCount)
            {
                return false;
            }

            int current = GetFruitAmount(fruitId);
            if (amount > int.MaxValue - current) return false;
            _fruitAmounts[fruitId] = current + amount;
            FruitCount += amount;
            Changed?.Invoke();
            return true;
        }

        public bool TryAddCapsules(int amount)
        {
            if (!CanCollect || amount <= 0 || amount > int.MaxValue - CapsuleCount)
            {
                return false;
            }

            CapsuleCount += amount;
            Changed?.Invoke();
            return true;
        }

        public bool TryConfirm()
        {
            if (State != TripSessionState.Succeeded)
            {
                return false;
            }

            State = TripSessionState.Confirmed;
            Changed?.Invoke();
            return true;
        }

        public bool TryCancel()
        {
            if (!CanCollect)
            {
                return false;
            }

            ClearResult();
            State = TripSessionState.Cancelled;
            Changed?.Invoke();
            return true;
        }

        private void ClearResult()
        {
            _fruitAmounts.Clear();
            _completedDeliveryIds.Clear();
            ActiveDeliveryId = null;
            Coins = 0;
            FruitCount = 0;
            CapsuleCount = 0;
        }
    }
}
