using ProjectSparks.Player;

namespace ProjectSparks.Collectibles
{
    public interface IFruitItem : IHoldableItem
    {
        string FruitId { get; }
        string DisplayName { get; }
    }
}
