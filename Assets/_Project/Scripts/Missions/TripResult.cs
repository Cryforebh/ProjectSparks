using System;
using ProjectSparks.Collectibles;

namespace ProjectSparks.Missions
{
    public sealed class TripResult
    {
        private readonly int[] _fruitAmounts = new int[5];

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
            for (int index = 0; index < _fruitAmounts.Length; index++)
            {
                _fruitAmounts[index] = session.GetFruitAmount((FruitType)index);
                FruitCount = checked(FruitCount + _fruitAmounts[index]);
            }
        }

        public string SessionId { get; }
        public int Coins { get; }
        public int FruitCount { get; }
        public int CapsuleCount { get; }

        public int GetFruitAmount(FruitType type)
        {
            return _fruitAmounts[(int)type];
        }
    }
}
