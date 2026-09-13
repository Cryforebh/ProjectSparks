using System;

namespace ProjectSparks.Missions
{
    public static class RandomMissionGenerator
    {
        public static bool TryGenerate(RandomMissionSettings settings, MissionScenePoints points,
            Random random, out MissionAssignment[] assignments, out string error)
        {
            assignments = null;
            if (settings == null || settings.DeliveryCount <= 0 || settings.Reward < 0 ||
                string.IsNullOrWhiteSpace(settings.MissionTitle))
            {
                error = "Назначьте настройки случайных заданий: положительное число доставок, название и неотрицательную награду.";
                return false;
            }

            if ((long)settings.DeliveryCount * settings.Reward > int.MaxValue)
            {
                error = "Суммарная награда за доставки превышает допустимое количество монет.";
                return false;
            }

            if (points == null || random == null)
            {
                error = "Для формирования заданий нужны список точек и источник случайных чисел.";
                return false;
            }

            if (!points.TryValidate(out error))
                return false;

            var pickups = new PointOrder(points.PickupCount, random);
            var deliveries = new PointOrder(points.DeliveryCount, random);
            string sequenceId = Guid.NewGuid().ToString("N");
            var result = new MissionAssignment[settings.DeliveryCount];
            for (int i = 0; i < result.Length; i++)
            {
                MissionScenePoint pickup = points.GetPickup(pickups.Next());
                MissionScenePoint delivery = points.GetDelivery(deliveries.Next());
                result[i] = new MissionAssignment($"{sequenceId}:{i + 1}",
                    $"{settings.MissionTitle} {i + 1}", settings.Reward,
                    pickup.Id, pickup.Label, delivery.Id, delivery.Label);
            }

            assignments = result;
            error = null;
            return true;
        }

        private sealed class PointOrder
        {
            private readonly int[] _indices;
            private readonly Random _random;
            private int _next;
            private int _last = -1;

            public PointOrder(int count, Random random)
            {
                _indices = new int[count];
                for (int i = 0; i < count; i++)
                    _indices[i] = i;
                _random = random;
                _next = count;
            }

            public int Next()
            {
                if (_next == _indices.Length)
                {
                    for (int i = _indices.Length - 1; i > 0; i--)
                    {
                        int other = _random.Next(i + 1);
                        Swap(i, other);
                    }

                    // На границе двух обходов не выбираем ту же точку подряд, если есть выбор.
                    if (_indices.Length > 1 && _indices[0] == _last)
                        Swap(0, _random.Next(1, _indices.Length));
                    _next = 0;
                }

                _last = _indices[_next++];
                return _last;
            }

            private void Swap(int first, int second)
            {
                int value = _indices[first];
                _indices[first] = _indices[second];
                _indices[second] = value;
            }
        }
    }
}
