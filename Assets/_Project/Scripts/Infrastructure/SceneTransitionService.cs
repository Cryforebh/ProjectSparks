using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using ProjectSparks.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectSparks.Infrastructure
{
    public sealed class SceneTransitionService : ISceneTransitionService, IDisposable
    {
        private const float MinimumLoadingScreenTime = 3f;
        private const int LoadingUploadTimeSlice = 8;

        private readonly ISceneLoader _sceneLoader;
        private readonly ISceneEntryPointService _entryPointService;
        private readonly IProgressService _progress;
        private readonly SceneFadeView _fadeView;
        private readonly CancellationTokenSource _lifetimeCancellation = new CancellationTokenSource();

        public SceneTransitionService(
            ISceneLoader sceneLoader,
            ISceneEntryPointService entryPointService,
            IProgressService progress,
            SceneFadeView fadeView)
        {
            _sceneLoader = sceneLoader;
            _entryPointService = entryPointService;
            _progress = progress;
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
                throw new InvalidOperationException("Переход уже выполняется.");
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

                _fadeView.PrepareLoadingScreen();
                await _fadeView.ShowLoadingScreenAsync(fadeCancellation.Token);
                float loadingScreenShownAt = Time.realtimeSinceStartup;
                await UniTask.NextFrame(cancellationToken: fadeCancellation.Token);
                // Искры, ящик фруктов и первая миссия читают уже загруженный прогресс в Start.
                await _progress.LoadAsync(fadeCancellation.Token);
                fadeCancellation.Token.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(spawnPointId))
                {
                    _entryPointService.Clear();
                }
                else
                {
                    _entryPointService.SetPending(spawnPointId);
                }

                await LoadBehindScreenAsync(sceneName, loadingScreenShownAt, _lifetimeCancellation.Token);
                await _fadeView.RevealSceneAsync(_lifetimeCancellation.Token);
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

        private async UniTask LoadBehindScreenAsync(string sceneName, float shownAt, CancellationToken cancellationToken)
        {
            int previousTimeSlice = QualitySettings.asyncUploadTimeSlice;
            // Пока сцена скрыта, Unity может загрузить больше текстур и мешей за кадр.
            QualitySettings.asyncUploadTimeSlice = Mathf.Max(previousTimeSlice, LoadingUploadTimeSlice);
            try
            {
                await _sceneLoader.LoadAsync(sceneName, _fadeView.SetProgress, cancellationToken);
                await WaitForSceneFrameAsync(cancellationToken);
                _fadeView.SetProgress(1f);

                float remainingTime = MinimumLoadingScreenTime - (Time.realtimeSinceStartup - shownAt);
                if (remainingTime > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(remainingTime), DelayType.Realtime,
                        cancellationToken: cancellationToken);
                }
            }
            finally
            {
                QualitySettings.asyncUploadTimeSlice = previousTimeSlice;
            }
        }

        private static async UniTask WaitForSceneFrameAsync(CancellationToken cancellationToken)
        {
            await UniTask.NextFrame(cancellationToken: cancellationToken);
            if (Application.isBatchMode)
            {
                return;
            }

            await UniTask.WaitForEndOfFrame(cancellationToken);
            // Чтение состояния этого GraphicsFence также требует поддержки async compute.
            if (SystemInfo.supportsGraphicsFence && SystemInfo.supportsAsyncCompute)
            {
                // Ждем отрисовку кадра новой сцены, пока непрозрачный экран еще виден.
                GraphicsFence fence = Graphics.CreateGraphicsFence(
                    GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.AllGPUOperations);
                await UniTask.WaitUntil(() => fence.passed, cancellationToken: cancellationToken);
            }
        }

        public void Dispose()
        {
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
        }
    }
}
