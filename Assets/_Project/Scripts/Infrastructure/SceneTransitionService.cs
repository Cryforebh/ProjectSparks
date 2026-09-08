using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using ProjectSparks.UI;

namespace ProjectSparks.Infrastructure
{
    public sealed class SceneTransitionService : ISceneTransitionService, IDisposable
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly ISceneEntryPointService _entryPointService;
        private readonly SceneFadeView _fadeView;
        private readonly CancellationTokenSource _lifetimeCancellation = new CancellationTokenSource();

        public SceneTransitionService(
            ISceneLoader sceneLoader,
            ISceneEntryPointService entryPointService,
            SceneFadeView fadeView)
        {
            _sceneLoader = sceneLoader;
            _entryPointService = entryPointService;
            _fadeView = fadeView;
        }

        public bool IsTransitioning { get; private set; }

        public async UniTask TransitionAsync(
            string sceneName,
            string spawnPointId,
            CancellationToken cancellationToken)
        {
            if (IsTransitioning)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException(
                    "Имя сцены перехода не может быть пустым.",
                    nameof(sceneName));
            }

            IsTransitioning = true;

            try
            {
                using CancellationTokenSource fadeCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        _lifetimeCancellation.Token);

                await _fadeView.FadeOutAsync(fadeCancellation.Token);
                fadeCancellation.Token.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(spawnPointId))
                {
                    _entryPointService.Clear();
                }
                else
                {
                    _entryPointService.SetPending(spawnPointId);
                }

                await _sceneLoader.LoadAsync(
                    sceneName,
                    _lifetimeCancellation.Token);
                await UniTask.Yield(_lifetimeCancellation.Token);
                await _fadeView.FadeInAsync(_lifetimeCancellation.Token);
            }
            catch
            {
                _entryPointService.Clear();
                _fadeView.ShowImmediately(false);

                throw;
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        public void Dispose()
        {
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
        }
    }
}
