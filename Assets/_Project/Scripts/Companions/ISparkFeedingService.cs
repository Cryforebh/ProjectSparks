using ProjectSparks.Collectibles;

namespace ProjectSparks.Companions
{
    public interface ISparkFeedingService
    {
        SparkStatValues GetIncrease(FruitType fruitType);
        bool CanFeed(CompanionType companionType, FruitType fruitType);
        bool TryFeed(CompanionType companionType, FruitType fruitType);
    }
}
