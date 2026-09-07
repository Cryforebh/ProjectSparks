namespace ProjectSparks.Collectibles
{
    public interface IFruitWorldService
    {
        bool TryTakeFromCrate(FruitType fruitType);
        bool TryPickUp(PhysicalFruitItem fruit);
        bool TryDropHeldFruit();
        bool TryReturnHeldFruitToCrate();
        bool TryConsumeHeldFruit();
    }
}
