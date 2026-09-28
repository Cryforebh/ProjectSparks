using System;
using System.Collections.Generic;
using ProjectSparks.Collectibles;

namespace ProjectSparks.Missions
{
    public sealed class TripResult
    {
        private readonly Dictionary<string, int> _fruitAmounts = new Dictionary<string, int>(StringComparer.Ordinal);

        public TripResult(ITripSessionService session)
        {
            if (session == null || session.State != TripSessionState.Succeeded ||
                string.IsNullOrWhiteSpace(session.SessionId))
            {
                throw new InvalidOperationException("Для сохранения нужна успешно завершенная поездка.");
            }

            SessionId = session.SessionId;
            Coins = session.Coins;
            CapsuleCount = session.CapsuleCount;
            foreach (KeyValuePair<string, int> pair in session.FruitAmounts)
            {
                _fruitAmounts.Add(pair.Key, pair.Value);
                FruitCount = checked(FruitCount + pair.Value);
            }
        }

        public string SessionId { get; }
        public int Coins { get; }
        public int FruitCount { get; }
        public int CapsuleCount { get; }
        public IReadOnlyDictionary<string, int> FruitAmounts => _fruitAmounts;

        public int GetFruitAmount(string fruitId)
        {
            return _fruitAmounts.TryGetValue(fruitId, out int amount) ? amount : 0;
        }
    }
}
