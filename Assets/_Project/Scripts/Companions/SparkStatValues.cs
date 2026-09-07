using System;

namespace ProjectSparks.Companions
{
    public readonly struct SparkStatValues
    {
        public SparkStatValues(
            int swim,
            int fly,
            int run,
            int power,
            int stamina)
        {
            Swim = Math.Max(0, swim);
            Fly = Math.Max(0, fly);
            Run = Math.Max(0, run);
            Power = Math.Max(0, power);
            Stamina = Math.Max(0, stamina);
        }

        public int Swim { get; }
        public int Fly { get; }
        public int Run { get; }
        public int Power { get; }
        public int Stamina { get; }

        public int GetValue(SparkStatType statType)
        {
            return statType switch
            {
                SparkStatType.Swim => Swim,
                SparkStatType.Fly => Fly,
                SparkStatType.Run => Run,
                SparkStatType.Power => Power,
                SparkStatType.Stamina => Stamina,
                _ => throw new ArgumentOutOfRangeException(nameof(statType))
            };
        }
    }
}
