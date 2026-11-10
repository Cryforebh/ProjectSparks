using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;

namespace ProjectSparks.Missions
{
    public sealed class TripExitService
    {
        private readonly ITripSessionService _session;
        private readonly ISceneTransitionService _transition;

        public TripExitService(ITripSessionService session, ISceneTransitionService transition)
        {
            _session = session;
            _transition = transition;
        }

        public bool IsBusy { get; private set; }
        public bool RequiresConfirmation => _session.State == TripSessionState.InProgress ||
            _session.State == TripSessionState.Succeeded;

        public async UniTask ExitAsync(string sceneName, CancellationToken cancellationToken)
        {
            if (IsBusy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("Не назначена сцена главного меню.", nameof(sceneName));
            }

            cancellationToken.ThrowIfCancellationRequested();
            IsBusy = true;
            try
            {
                // После начала загрузки уничтожение старой сцены не отменяет завершение выхода.
                await _transition.TransitionAsync(sceneName, string.Empty, cancellationToken);
                _session.TryCancel();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
