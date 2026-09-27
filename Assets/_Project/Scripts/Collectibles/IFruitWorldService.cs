using ProjectSparks.Companions;
using UnityEngine;

namespace ProjectSparks.Collectibles
{
    public interface IFruitWorldService
    {
        bool TryTakeFromCrate(FruitType fruitType);
        bool TryPickUp(PhysicalFruitItem fruit);
        bool TryDropHeldFruit();
        bool TryReturnHeldFruitToCrate();
        bool TryConsumeHeldFruit();
        bool TryPresentHeldFruit(Transform anchor, ISparkStateController spark);
    }
}
