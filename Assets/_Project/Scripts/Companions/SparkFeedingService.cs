using ProjectSparks.Collectibles;

namespace ProjectSparks.Companions
{
    public sealed class SparkFeedingService : ISparkFeedingService
    {
        private const int PrimaryIncrease = 5;
        private const int SecondaryIncrease = 2;

        private readonly ISparkProgressService _sparkProgressService;

        public SparkFeedingService(ISparkProgressService sparkProgressService)
        {
            _sparkProgressService = sparkProgressService;
        }

        public SparkStatValues GetIncrease(FruitType fruitType)
        {
            return new SparkStatValues(
                fruitType == FruitType.Swim ? PrimaryIncrease : SecondaryIncrease,
                fruitType == FruitType.Fly ? PrimaryIncrease : SecondaryIncrease,
                fruitType == FruitType.Run ? PrimaryIncrease : SecondaryIncrease,
                fruitType == FruitType.Power ? PrimaryIncrease : SecondaryIncrease,
                fruitType == FruitType.Stamina ? PrimaryIncrease : SecondaryIncrease);
        }

        public bool CanFeed(CompanionType companionType, FruitType fruitType)
        {
            return companionType != CompanionType.None;
        }

        public bool TryFeed(CompanionType companionType, FruitType fruitType)
        {
            if (!CanFeed(companionType, fruitType))
            {
                return false;
            }

            return _sparkProgressService.AddStats(
                companionType,
                GetIncrease(fruitType));
        }
    }
}
