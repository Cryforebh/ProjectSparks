using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Companions;
using ProjectSparks.Core;

namespace ProjectSparks.Levels
{
    public sealed class LevelSelectionService
    {
        private readonly IProgressService _progress;
        private readonly ICompanionService _companions;
        private readonly ISparkProgressService _sparkProgress;
        private readonly ISceneTransitionService _transition;

        public event Action OpenRequested;
        public LevelCatalog Catalog { get; }
        public bool IsBusy { get; private set; }
        public bool HasAvailableSparks => _progress.IsLoaded && _sparkProgress.GetAll().Length > 0;
        public SparkProgressData[] AvailableSparks => _sparkProgress.GetAll();

        public LevelSelectionService(LevelCatalog catalog, IProgressService progress,
            ICompanionService companions, ISparkProgressService sparkProgress,
            ISceneTransitionService transition)
        {
            Catalog = catalog;
            _progress = progress;
            _companions = companions;
            _sparkProgress = sparkProgress;
            _transition = transition;
        }

        public void Open()
        {
            if (!IsBusy && !_transition.IsTransitioning)
                OpenRequested?.Invoke();
        }

        public UniTask PrepareAsync(CancellationToken cancellationToken) => _progress.LoadAsync(cancellationToken);

        public async UniTask LaunchAsync(LevelDefinition level, string sparkId,
            CancellationToken cancellationToken)
        {
            if (IsBusy || _transition.IsTransitioning)
                throw new InvalidOperationException("Переход уже выполняется.");
            if (!Catalog.CanLaunch(level))
                throw new InvalidOperationException("Эта локация пока недоступна.");
            if (string.IsNullOrWhiteSpace(sparkId))
                throw new ArgumentException("Для поездки нужно выбрать Искру.", nameof(sparkId));

            IsBusy = true;
            try
            {
                await _progress.LoadAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!_sparkProgress.Contains(sparkId))
                    throw new InvalidOperationException("Выбранная Искра недоступна.");
                if (_companions.SelectedCompanion != sparkId)
                    _companions.Select(sparkId);
                await _transition.TransitionAsync(level.SceneName, string.Empty, cancellationToken);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
