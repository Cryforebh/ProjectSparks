using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;

namespace ProjectSparks.Missions
{
    public sealed class TripExitService
    {
        private readonly ITripSessionService _session;
        private readonly ISceneLoader _sceneLoader;

        public TripExitService(ITripSessionService session, ISceneLoader sceneLoader)
        {
            _session = session;
            _sceneLoader = sceneLoader;
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
                await _sceneLoader.LoadAsync(sceneName, CancellationToken.None);
                _session.TryCancel();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
