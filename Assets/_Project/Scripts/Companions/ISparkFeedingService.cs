using ProjectSparks.Collectibles;

namespace ProjectSparks.Companions
{
    public interface ISparkFeedingService
    {
        SparkStatValues GetIncrease(string fruitId);
        bool CanFeed(string sparkId, string fruitId);
        bool TryFeed(string sparkId, string fruitId);
    }
}
