namespace ProjectSparks.Companions
{
    public interface ISparkProfile
    {
        string DisplayName { get; }
        string AbilityDescription { get; }
        CompanionType CompanionType { get; }
    }
}
