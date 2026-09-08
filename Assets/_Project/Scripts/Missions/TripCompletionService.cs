using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;

namespace ProjectSparks.Missions
{
    public sealed class TripCompletionService
    {
        private readonly ITripSessionService _session;
        private readonly IProgressService _progress;
        private readonly ISceneTransitionService _transition;

        public TripCompletionService(ITripSessionService session, IProgressService progress,
            ISceneTransitionService transition)
        {
            _session = session;
            _progress = progress;
            _transition = transition;
        }

        public bool IsBusy { get; private set; }

        public async UniTask CompleteAsync(string sceneName, CancellationToken cancellationToken)
        {
            if (IsBusy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName) || _transition.IsTransitioning)
            {
                throw new InvalidOperationException("Сейчас нельзя начать переход в убежище.");
            }

            IsBusy = true;
            try
            {
                if (_session.State == TripSessionState.Succeeded)
                {
                    await _progress.CommitTripAsync(new TripResult(_session), cancellationToken);
                    if (!_session.TryConfirm())
                    {
                        throw new InvalidOperationException("Не удалось подтвердить сохраненную поездку.");
                    }
                }
                else if (_session.State != TripSessionState.Confirmed)
                {
                    throw new InvalidOperationException("Сначала завершите все доставки.");
                }

                await _transition.TransitionAsync(sceneName, string.Empty, cancellationToken);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
