using ProjectSparks.Collectibles;

namespace ProjectSparks.Companions
{
    public sealed class SparkFeedingService : ISparkFeedingService
    {
        private readonly SparkBalanceSettings _settings;
        private readonly FruitCatalog _catalog;

        private readonly ISparkProgressService _sparkProgressService;

        public SparkFeedingService(ISparkProgressService sparkProgressService,
            SparkBalanceSettings settings, FruitCatalog catalog)
        {
            _sparkProgressService = sparkProgressService;
            _settings = settings;
            _catalog = catalog;
        }

        public SparkStatValues GetIncrease(string fruitId)
        {
            if (!_catalog.TryGetEntry(fruitId, out FruitCatalog.Entry entry))
                throw new System.ArgumentException("Фрукт отсутствует в каталоге.", nameof(fruitId));
            return entry.StatIncrease;
        }

        public bool CanFeed(string sparkId, string fruitId)
        {
            return _sparkProgressService.Contains(sparkId) && _catalog.Contains(fruitId);
        }

        public bool TryFeed(string sparkId, string fruitId)
        {
            if (!CanFeed(sparkId, fruitId))
            {
                return false;
            }

            return _sparkProgressService.AddStats(
                sparkId,
                GetIncrease(fruitId),
                _settings.GrowthPerFeeding);
        }
    }
}
