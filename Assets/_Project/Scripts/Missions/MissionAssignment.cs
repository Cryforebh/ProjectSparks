using System;

namespace ProjectSparks.Missions
{
    public sealed class MissionAssignment
    {
        public string Id { get; }
        public string Title { get; }
        public int Reward { get; }
        public string PickupPointId { get; }
        public string PickupLabel { get; }
        public string DeliveryPointId { get; }
        public string DestinationLabel { get; }

        public MissionAssignment(string id, string title, int reward,
            string pickupPointId, string pickupLabel, string deliveryPointId, string destinationLabel)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Идентификатор задания не может быть пустым.", nameof(id));
            if (reward < 0)
                throw new ArgumentOutOfRangeException(nameof(reward), "Награда не может быть отрицательной.");
            if (string.IsNullOrWhiteSpace(pickupPointId) || string.IsNullOrWhiteSpace(deliveryPointId) ||
                string.Equals(pickupPointId, deliveryPointId, StringComparison.Ordinal))
                throw new ArgumentException("Нужны два разных непустых идентификатора точек.");
            if (string.IsNullOrWhiteSpace(pickupLabel) || string.IsNullOrWhiteSpace(destinationLabel))
                throw new ArgumentException("У обеих точек должны быть названия.");

            Id = id;
            Title = title;
            Reward = reward;
            PickupPointId = pickupPointId;
            PickupLabel = pickupLabel;
            DeliveryPointId = deliveryPointId;
            DestinationLabel = destinationLabel;
        }
    }
}
