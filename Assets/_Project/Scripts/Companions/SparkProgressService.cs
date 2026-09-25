using System;
using System.Collections.Generic;

namespace ProjectSparks.Companions
{
    public sealed class SparkProgressService : ISparkProgressService
    {
        private const int MaxNameLength = 24;

        private readonly Dictionary<string, SparkProgressData> _progressById =
            new Dictionary<string, SparkProgressData>();

        private readonly SparkStarterSettings _starterSettings;

        public SparkProgressService(SparkStarterSettings starterSettings)
        {
            _starterSettings = starterSettings;
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

        public bool AddStat(string sparkId, SparkStatType statType, int amount)
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

        public bool AddStats(string sparkId, SparkStatValues amounts, float growthIncrease = 0f)
        {
            SparkProgressData current = GetProgress(sparkId);

            bool hasStatIncrease =
                amounts.Swim != 0
                || amounts.Fly != 0
                || amounts.Run != 0
                || amounts.Power != 0
                || amounts.Stamina != 0;

            float nextGrowth = Math.Min(
                1f,
                current.Growth + Math.Max(0f, growthIncrease));

            bool hasGrowthIncrease = nextGrowth > current.Growth;

            if (!hasStatIncrease && !hasGrowthIncrease)
            {
                return false;
            }

            _progressById[sparkId] = CopyWith(
                current,
                current.DisplayName,
                amounts,
                nextGrowth);

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
                progress.GetStat(SparkStatType.Stamina),
                progress.BodyColor,
                progress.Growth,
                progress.Emotion);

            SparkChanged?.Invoke(progress.Id);
        }

        public void Reset()
        {
            ResetValues();

            foreach (string sparkId in _progressById.Keys)
            {
                SparkChanged?.Invoke(sparkId);
            }
        }

        private static SparkProgressData CopyWith(
            SparkProgressData current,
            string displayName,
            SparkStatValues amounts,
            float? growth = null)
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
                GetValue(SparkStatType.Stamina),
                current.BodyColor,
                growth ?? current.Growth,
                current.Emotion);
        }

        private void ResetValues()
        {
            _progressById.Clear();

            foreach (SparkDefinition definition in _starterSettings.Definitions)
            {
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        "В SparkStarterSettings обнаружен пустой SparkDefinition.");
                }

                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"У SparkDefinition '{definition.name}' не задан ID.");
                }

                if (_progressById.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"Дублирующийся ID Искры: '{definition.Id}'.");
                }

                _progressById.Add(definition.Id, CreateDefault(definition));
            }
        }

        private static SparkProgressData CreateDefault(SparkDefinition definition)
        {
            SparkStatValues stats = definition.InitialStats;

            return new SparkProgressData(
                definition.Id,
                definition.DefaultName,
                stats.Swim,
                stats.Fly,
                stats.Run,
                stats.Power,
                stats.Stamina,
                definition.BodyColor,
                growth: 0f,
                emotion: definition.Emotion);
        }

        private string GetDefaultName(string sparkId)
        {
            foreach (SparkDefinition definition in _starterSettings.Definitions)
            {
                if (definition != null && definition.Id == sparkId)
                {
                    return definition.DefaultName;
                }
            }

            return "Искра";
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
