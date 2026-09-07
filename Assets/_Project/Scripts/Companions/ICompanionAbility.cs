namespace ProjectSparks.Companions
{
    public interface ICompanionAbility
    {
        CompanionType CompanionType { get; }

        float ModifyPickupRadius(float baseRadius);
        float ModifyBoostConsumption(float baseConsumption);
    }
}
