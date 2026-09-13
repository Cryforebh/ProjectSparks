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
        private readonly ISceneTransitionService _transition;

        public event Action OpenRequested;
        public LevelCatalog Catalog { get; }
        public bool IsBusy { get; private set; }
        public bool HasCompanion => _progress.IsLoaded && _companions.SelectedCompanion != string.Empty;

        public LevelSelectionService(LevelCatalog catalog, IProgressService progress,
            ICompanionService companions, ISceneTransitionService transition)
        {
            Catalog = catalog;
            _progress = progress;
            _companions = companions;
            _transition = transition;
        }

        public void Open()
        {
            if (!IsBusy && !_transition.IsTransitioning)
                OpenRequested?.Invoke();
        }

        public UniTask PrepareAsync(CancellationToken cancellationToken) => _progress.LoadAsync(cancellationToken);

        public async UniTask LaunchAsync(LevelDefinition level, CancellationToken cancellationToken)
        {
            if (IsBusy || _transition.IsTransitioning)
                throw new InvalidOperationException("Переход уже выполняется.");
            if (!Catalog.CanLaunch(level))
                throw new InvalidOperationException("Эта локация пока недоступна.");

            IsBusy = true;
            try
            {
                await _progress.LoadAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!HasCompanion)
                    throw new InvalidOperationException("Сначала выберите Искру в убежище.");
                await _transition.TransitionAsync(level.SceneName, string.Empty, cancellationToken);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
