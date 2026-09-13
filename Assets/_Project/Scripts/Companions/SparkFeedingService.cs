using ProjectSparks.Collectibles;

namespace ProjectSparks.Companions
{
    public sealed class SparkFeedingService : ISparkFeedingService
    {
        private readonly SparkBalanceSettings _settings;

        private readonly ISparkProgressService _sparkProgressService;

        public SparkFeedingService(ISparkProgressService sparkProgressService, SparkBalanceSettings settings)
        {
            _sparkProgressService = sparkProgressService;
            _settings = settings;
        }

        public SparkStatValues GetIncrease(FruitType fruitType)
        {
            return _settings.GetFruitIncrease(fruitType);
        }

        public bool CanFeed(string sparkId, FruitType fruitType)
        {
            return _sparkProgressService.Contains(sparkId) && System.Enum.IsDefined(typeof(FruitType), fruitType);
        }

        public bool TryFeed(string sparkId, FruitType fruitType)
        {
            if (!CanFeed(sparkId, fruitType))
            {
                return false;
            }

            return _sparkProgressService.AddStats(
                sparkId,
                GetIncrease(fruitType));
        }
    }
}
