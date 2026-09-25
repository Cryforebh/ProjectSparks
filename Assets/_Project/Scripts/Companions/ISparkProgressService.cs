using System;

namespace ProjectSparks.Companions
{
    public interface ISparkProgressService
    {
        event Action<string> SparkChanged;

        SparkProgressData GetProgress(string sparkId);
        bool Rename(string sparkId, string displayName);
        bool AddStat(string sparkId, SparkStatType statType, int amount);
        bool AddStats(string sparkId, SparkStatValues amounts, float growthIncrease = 0f);
        void Restore(SparkProgressData progress);
        void Reset();
        bool Contains(string id);
        SparkProgressData[] GetAll();
    }
}
