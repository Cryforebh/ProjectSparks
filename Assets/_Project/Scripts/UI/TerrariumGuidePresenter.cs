using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.UI
{
    public sealed class TerrariumGuidePresenter : IInitializable, IDisposable
    {
        private readonly IProgressService _progress;
        private readonly IPauseService _pause;
        private readonly ISceneTransitionService _transition;
        private readonly LazyInject<TerrariumGuideView> _guide;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private TerrariumGuideView _view;

        public TerrariumGuidePresenter(IProgressService progress, IPauseService pause,
            ISceneTransitionService transition, LazyInject<TerrariumGuideView> guide)
        {
            _progress = progress;
            _pause = pause;
            _transition = transition;
            _guide = guide;
        }

        public void Initialize()
        {
            OpenOnFirstVisitAsync(_lifetime.Token).Forget();
        }

        public void Dispose()
        {
            Unsubscribe();
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        private async UniTask OpenOnFirstVisitAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _progress.LoadAsync(cancellationToken);
                if (_progress.TerrariumGuideSeen)
                {
                    return;
                }

                await UniTask.WaitUntil(
                    () => !_transition.IsTransitioning && !_pause.IsPaused,
                    cancellationToken: cancellationToken);
                _view = _guide.Value;
                _view.Show();
                _view.Completed += HandleCompleted;
                _pause.PauseChanged += KeepGuidePaused;
                _pause.SetPaused(true, false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void KeepGuidePaused(bool isPaused)
        {
            if (!isPaused || _pause.IsPauseMenuVisible)
            {
                _pause.SetPaused(true, false);
            }
        }

        private void HandleCompleted()
        {
            CompleteAsync(_lifetime.Token).Forget();
        }

        private async UniTask CompleteAsync(CancellationToken cancellationToken)
        {
            _view.SetSaving();
            try
            {
                await _progress.CompleteTerrariumGuideAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                Unsubscribe();
                _view.Hide();
                _pause.SetPaused(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (_view != null && !cancellationToken.IsCancellationRequested)
                {
                    _view.ShowSaveError();
                }
            }
        }

        private void Unsubscribe()
        {
            _pause.PauseChanged -= KeepGuidePaused;
            if (_view != null)
            {
                _view.Completed -= HandleCompleted;
            }
        }
    }
}
