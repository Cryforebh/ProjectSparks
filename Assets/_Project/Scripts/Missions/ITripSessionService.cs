using System;
using System.Collections.Generic;
using ProjectSparks.Collectibles;

namespace ProjectSparks.Missions
{
    public interface ITripSessionService
    {
        event Action Changed;

        string LocationId { get; }
        string SessionId { get; }
        string ActiveDeliveryId { get; }
        TripSessionState State { get; }
        int RequiredDeliveryCount { get; }
        int CompletedDeliveryCount { get; }
        int Coins { get; }
        int FruitCount { get; }
        int CapsuleCount { get; }
        IReadOnlyDictionary<string, int> FruitAmounts { get; }

        int GetFruitAmount(string fruitId);
        bool TryBegin(string locationId, int requiredDeliveryCount);
        bool TryBeginDelivery(string deliveryId);
        bool TryCompleteDelivery(string deliveryId, int coinReward);
        bool TryAddCoins(int amount);
        bool TryAddFruit(string fruitId, int amount);
        bool TryAddCapsules(int amount);

        // Фиксирует состояние модели; перенос наград и сохранение выполняются отдельно.
        bool TryConfirm();
        bool TryCancel();
    }
}
