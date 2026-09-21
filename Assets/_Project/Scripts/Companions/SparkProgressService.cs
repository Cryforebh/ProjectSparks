using System;
using System.Collections.Generic;

namespace ProjectSparks.Companions
{
    public sealed class SparkProgressService : ISparkProgressService
    {
        private const int MaxNameLength = 24;

        private readonly Dictionary<string, SparkProgressData> _progressById =
            new Dictionary<string, SparkProgressData>();

        private readonly SparkBalanceSettings _balance;

        public SparkProgressService(SparkBalanceSettings balance)
        {
            _balance = balance;
            ResetValues();
        }

        public event Action<string> SparkChanged;
        public bool Contains(string id) => !string.IsNullOrEmpty(id) && _progressById.ContainsKey(id);
        public SparkProgressData[] GetAll()
        {
            var values = new SparkProgressData[_progressById.Count];
            _progressById.Values.CopyTo(values, 0);
            return values;
        }

        public SparkProgressData GetProgress(string sparkId)
        {
            if (!_progressById.TryGetValue(sparkId, out SparkProgressData progress))
            {
                throw new ArgumentOutOfRangeException(nameof(sparkId));
            }

            return progress;
        }

        public bool Rename(string sparkId, string displayName)
        {
            SparkProgressData current = GetProgress(sparkId);
            string normalizedName = NormalizeName(displayName);
            if (string.IsNullOrEmpty(normalizedName)
                || current.DisplayName == normalizedName)
            {
                return false;
            }

            _progressById[sparkId] = CopyWith(
                current,
                normalizedName,
                new SparkStatValues());
            SparkChanged?.Invoke(sparkId);
            return true;
        }

        public bool AddStat(
            string sparkId,
            SparkStatType statType,
            int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            SparkProgressData current = GetProgress(sparkId);
            _progressById[sparkId] = CopyWith(
                current,
                current.DisplayName,
                new SparkStatValues(
                    statType == SparkStatType.Swim ? amount : 0,
                    statType == SparkStatType.Fly ? amount : 0,
                    statType == SparkStatType.Run ? amount : 0,
                    statType == SparkStatType.Power ? amount : 0,
                    statType == SparkStatType.Stamina ? amount : 0));
            SparkChanged?.Invoke(sparkId);
            return true;
        }

        public bool AddStats(
            string sparkId,
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

            SparkProgressData current = GetProgress(sparkId);
            _progressById[sparkId] = CopyWith(
                current,
                current.DisplayName,
                amounts);
            SparkChanged?.Invoke(sparkId);
            return true;
        }

        public void Restore(SparkProgressData progress)
        {
            if (progress == null
                || string.IsNullOrWhiteSpace(progress.Id)
)
            {
                return;
            }

            string restoredName = NormalizeName(progress.DisplayName);
            if (string.IsNullOrEmpty(restoredName))
            {
                restoredName = GetDefaultName(progress.Id);
            }

            _progressById[progress.Id] = new SparkProgressData(
                progress.Id,
                restoredName,
                progress.GetStat(SparkStatType.Swim),
                progress.GetStat(SparkStatType.Fly),
                progress.GetStat(SparkStatType.Run),
                progress.GetStat(SparkStatType.Power),
                progress.GetStat(SparkStatType.Stamina), progress.BodyColor);
            SparkChanged?.Invoke(progress.Id);
        }

        public void Reset()
        {
            ResetValues();
            SparkChanged?.Invoke(SparkIds.First);
            SparkChanged?.Invoke(SparkIds.Second);
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
                current.Id,
                displayName,
                GetValue(SparkStatType.Swim),
                GetValue(SparkStatType.Fly),
                GetValue(SparkStatType.Run),
                GetValue(SparkStatType.Power),
                GetValue(SparkStatType.Stamina), current.BodyColor);
        }

        private void ResetValues()
        {
            _progressById.Clear();
            _progressById[SparkIds.First] = CreateDefault(
                SparkIds.First);
            _progressById[SparkIds.Second] = CreateDefault(
                SparkIds.Second);
        }

        private SparkProgressData CreateDefault(string sparkId)
        {
            SparkAbilityGrowth growth = sparkId == SparkIds.First ? _balance.PickupRadius : _balance.BoostSaving;
            int Value(SparkStatType stat) => growth.Stat == stat ? growth.UnlockAt : 0;
            return new SparkProgressData(sparkId, GetDefaultName(sparkId),
                Value(SparkStatType.Swim), Value(SparkStatType.Fly), Value(SparkStatType.Run),
                Value(SparkStatType.Power), Value(SparkStatType.Stamina),
                sparkId == SparkIds.First ? UnityEngine.Color.white : new UnityEngine.Color(0.1f, 0.75f, 0.75f, 1f));
        }

        private static string GetDefaultName(string sparkId)
        {
            return sparkId switch
            {
                SparkIds.First => "Искра 1",
                SparkIds.Second => "Искра 2",
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
