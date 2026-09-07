using ProjectSparks.Player;

namespace ProjectSparks.Collectibles
{
    public interface IFruitItem : IHoldableItem
    {
        FruitType FruitType { get; }
        string DisplayName { get; }
    }
}
