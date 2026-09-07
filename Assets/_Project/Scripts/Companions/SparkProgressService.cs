using System;
using System.Collections.Generic;

namespace ProjectSparks.Companions
{
    public sealed class SparkProgressService : ISparkProgressService
    {
        private const int MaxNameLength = 24;

        private readonly Dictionary<CompanionType, SparkProgressData> _progressByType =
            new Dictionary<CompanionType, SparkProgressData>();

        public SparkProgressService()
        {
            ResetValues();
        }

        public event Action<CompanionType> SparkChanged;

        public SparkProgressData GetProgress(CompanionType companionType)
        {
            if (!_progressByType.TryGetValue(companionType, out SparkProgressData progress))
            {
                throw new ArgumentOutOfRangeException(nameof(companionType));
            }

            return progress;
        }

        public bool Rename(CompanionType companionType, string displayName)
        {
            SparkProgressData current = GetProgress(companionType);
            string normalizedName = NormalizeName(displayName);
            if (string.IsNullOrEmpty(normalizedName)
                || current.DisplayName == normalizedName)
            {
                return false;
            }

            _progressByType[companionType] = CopyWith(
                current,
                normalizedName,
                new SparkStatValues());
            SparkChanged?.Invoke(companionType);
            return true;
        }

        public bool AddStat(
            CompanionType companionType,
            SparkStatType statType,
            int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            SparkProgressData current = GetProgress(companionType);
            _progressByType[companionType] = CopyWith(
                current,
                current.DisplayName,
                new SparkStatValues(
                    statType == SparkStatType.Swim ? amount : 0,
                    statType == SparkStatType.Fly ? amount : 0,
                    statType == SparkStatType.Run ? amount : 0,
                    statType == SparkStatType.Power ? amount : 0,
                    statType == SparkStatType.Stamina ? amount : 0));
            SparkChanged?.Invoke(companionType);
            return true;
        }

        public bool AddStats(
            CompanionType companionType,
            SparkStatValues amounts)
        {
            if (amounts.Swim == 0
                && amounts.Fly == 0
                && amounts.Run == 0
                && amounts.Power == 0
                && amounts.Stamina == 0)
            {
                return false;
            }

            SparkProgressData current = GetProgress(companionType);
            _progressByType[companionType] = CopyWith(
                current,
                current.DisplayName,
                amounts);
            SparkChanged?.Invoke(companionType);
            return true;
        }

        public void Restore(SparkProgressData progress)
        {
            if (progress == null
                || progress.CompanionType == CompanionType.None
                || !_progressByType.ContainsKey(progress.CompanionType))
            {
                return;
            }

            string restoredName = NormalizeName(progress.DisplayName);
            if (string.IsNullOrEmpty(restoredName))
            {
                restoredName = GetDefaultName(progress.CompanionType);
            }

            _progressByType[progress.CompanionType] = new SparkProgressData(
                progress.CompanionType,
                restoredName,
                progress.GetStat(SparkStatType.Swim),
                progress.GetStat(SparkStatType.Fly),
                progress.GetStat(SparkStatType.Run),
                progress.GetStat(SparkStatType.Power),
                progress.GetStat(SparkStatType.Stamina));
            SparkChanged?.Invoke(progress.CompanionType);
        }

        public void Reset()
        {
            ResetValues();
            SparkChanged?.Invoke(CompanionType.Collector);
            SparkChanged?.Invoke(CompanionType.Mechanic);
        }

        private static SparkProgressData CopyWith(
            SparkProgressData current,
            string displayName,
            SparkStatValues amounts)
        {
            int GetValue(SparkStatType statType)
            {
                int currentValue = current.GetStat(statType);
                long newValue = (long)currentValue + amounts.GetValue(statType);
                return (int)Math.Min(int.MaxValue, newValue);
            }

            return new SparkProgressData(
                current.CompanionType,
                displayName,
                GetValue(SparkStatType.Swim),
                GetValue(SparkStatType.Fly),
                GetValue(SparkStatType.Run),
                GetValue(SparkStatType.Power),
                GetValue(SparkStatType.Stamina));
        }

        private void ResetValues()
        {
            _progressByType[CompanionType.Collector] = CreateDefault(
                CompanionType.Collector);
            _progressByType[CompanionType.Mechanic] = CreateDefault(
                CompanionType.Mechanic);
        }

        private static SparkProgressData CreateDefault(CompanionType companionType)
        {
            return new SparkProgressData(
                companionType,
                GetDefaultName(companionType),
                0,
                0,
                0,
                0,
                0);
        }

        private static string GetDefaultName(CompanionType companionType)
        {
            return companionType switch
            {
                CompanionType.Collector => "Искра-Собиратель",
                CompanionType.Mechanic => "Искра-Механик",
                _ => "Искра"
            };
        }

        private static string NormalizeName(string displayName)
        {
            string normalizedName = displayName?.Trim() ?? string.Empty;
            return normalizedName.Length > MaxNameLength
                ? normalizedName.Substring(0, MaxNameLength)
                : normalizedName;
        }
    }
}
