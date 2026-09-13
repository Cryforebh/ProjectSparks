using System;
namespace ProjectSparks.Companions
{
    public sealed class CompanionService : ICompanionService, IDisposable
    {
        private readonly ISparkProgressService _progress;
        private readonly SparkDevelopmentCalculator _calculator;
        public CompanionService(ISparkProgressService progress, SparkDevelopmentCalculator calculator)
        {
            _progress = progress;
            _calculator = calculator;
            _progress.SparkChanged += HandleSparkChanged;
        }
        public event Action<string> SelectedCompanionChanged;
        public string SelectedCompanion { get; private set; } = string.Empty;
        public bool Select(string sparkId)
        {
            if (!_progress.Contains(sparkId) || SelectedCompanion == sparkId) return false;
            SelectedCompanion = sparkId;
            SelectedCompanionChanged?.Invoke(sparkId);
            return true;
        }
        public void Restore(string sparkId)
        {
            string restored = _progress.Contains(sparkId) ? sparkId : string.Empty;
            if (restored == SelectedCompanion) return;
            SelectedCompanion = restored;
            SelectedCompanionChanged?.Invoke(restored);
        }
        private SparkAbilityEffects Effects => _progress.Contains(SelectedCompanion)
            ? _calculator.Evaluate(_progress.GetProgress(SelectedCompanion).Stats)
            : new SparkAbilityEffects(1f, 1f);
        public float ModifyPickupRadius(float baseRadius) => Math.Max(0f, baseRadius) * Effects.PickupRadiusMultiplier;
        public float ModifyBoostConsumption(float baseConsumption) => Math.Max(0f, baseConsumption) * Effects.BoostConsumptionMultiplier;
        private void HandleSparkChanged(string id)
        {
            if (id == SelectedCompanion) SelectedCompanionChanged?.Invoke(id);
        }
        public void Dispose() => _progress.SparkChanged -= HandleSparkChanged;
    }
}
