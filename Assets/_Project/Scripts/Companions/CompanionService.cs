using System;
using System.Collections.Generic;

namespace ProjectSparks.Companions
{
    public sealed class CompanionService : ICompanionService
    {
        private readonly Dictionary<CompanionType, ICompanionAbility> _abilities;

        public CompanionService(List<ICompanionAbility> abilities)
        {
            _abilities = new Dictionary<CompanionType, ICompanionAbility>();

            foreach (ICompanionAbility ability in abilities)
            {
                if (ability == null || ability.CompanionType == CompanionType.None)
                {
                    throw new ArgumentException(
                        "Способность компаньона должна иметь допустимый тип.",
                        nameof(abilities));
                }

                if (_abilities.ContainsKey(ability.CompanionType))
                {
                    throw new ArgumentException(
                        $"Для компаньона {ability.CompanionType} зарегистрировано несколько способностей.",
                        nameof(abilities));
                }

                _abilities.Add(ability.CompanionType, ability);
            }
        }

        public event Action<CompanionType> SelectedCompanionChanged;

        public CompanionType SelectedCompanion { get; private set; }

        public bool Select(CompanionType companionType)
        {
            if (!IsAvailable(companionType) || SelectedCompanion == companionType)
            {
                return false;
            }

            SelectedCompanion = companionType;
            SelectedCompanionChanged?.Invoke(SelectedCompanion);
            return true;
        }

        public void Restore(CompanionType companionType)
        {
            CompanionType restoredCompanion = IsAvailable(companionType)
                ? companionType
                : CompanionType.None;

            if (SelectedCompanion == restoredCompanion)
            {
                return;
            }

            SelectedCompanion = restoredCompanion;
            SelectedCompanionChanged?.Invoke(SelectedCompanion);
        }

        public float ModifyPickupRadius(float baseRadius)
        {
            return TryGetSelectedAbility(out ICompanionAbility ability)
                ? ability.ModifyPickupRadius(baseRadius)
                : Math.Max(0f, baseRadius);
        }

        public float ModifyBoostConsumption(float baseConsumption)
        {
            return TryGetSelectedAbility(out ICompanionAbility ability)
                ? ability.ModifyBoostConsumption(baseConsumption)
                : Math.Max(0f, baseConsumption);
        }

        private bool IsAvailable(CompanionType companionType)
        {
            return companionType == CompanionType.None || _abilities.ContainsKey(companionType);
        }

        private bool TryGetSelectedAbility(out ICompanionAbility ability)
        {
            return _abilities.TryGetValue(SelectedCompanion, out ability);
        }
    }
}
