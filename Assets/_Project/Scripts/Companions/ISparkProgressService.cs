using System;

namespace ProjectSparks.Companions
{
    public interface ISparkProgressService
    {
        event Action<CompanionType> SparkChanged;

        SparkProgressData GetProgress(CompanionType companionType);
        bool Rename(CompanionType companionType, string displayName);
        bool AddStat(CompanionType companionType, SparkStatType statType, int amount);
        bool AddStats(CompanionType companionType, SparkStatValues amounts);
        void Restore(SparkProgressData progress);
        void Reset();
    }
}
