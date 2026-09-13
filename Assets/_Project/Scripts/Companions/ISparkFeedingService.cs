using ProjectSparks.Collectibles;

namespace ProjectSparks.Companions
{
    public interface ISparkFeedingService
    {
        SparkStatValues GetIncrease(FruitType fruitType);
        bool CanFeed(string sparkId, FruitType fruitType);
        bool TryFeed(string sparkId, FruitType fruitType);
    }
}
