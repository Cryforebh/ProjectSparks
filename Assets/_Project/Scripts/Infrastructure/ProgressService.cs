using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using UnityEngine;

namespace ProjectSparks.Infrastructure
{
    public sealed class ProgressService : IProgressService, IDisposable
    {
        private readonly ISaveService _saveService;
        private readonly IEconomyService _economyService;
        private readonly CancellationTokenSource _lifetimeCancellation = new CancellationTokenSource();
        private readonly SemaphoreSlim _saveOperationLock = new SemaphoreSlim(1, 1);

        private bool _isSubscribed;
        private bool _isSaving;
        private bool _saveRequested;
        private bool _ignoreBalanceChanges;

        public ProgressService(
            ISaveService saveService,
            IEconomyService economyService)
        {
            _saveService = saveService;
            _economyService = economyService;
        }

        public bool IsLoaded { get; private set; }

        public async UniTask LoadAsync(CancellationToken cancellationToken)
        {
            if (IsLoaded)
            {
                return;
            }

            using CancellationTokenSource linkedCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _lifetimeCancellation.Token);

            GameProgressData progress;
            try
            {
                progress = await _saveService.LoadAsync(linkedCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                progress = new GameProgressData(0);
            }

            _economyService.Restore(progress.Balance);
            IsLoaded = true;
            Subscribe();
        }

        public async UniTask ResetAsync(CancellationToken cancellationToken)
        {
            using CancellationTokenSource linkedCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _lifetimeCancellation.Token);

            await _saveOperationLock.WaitAsync(linkedCancellation.Token);
            try
            {
                _ignoreBalanceChanges = true;
                _economyService.Restore(0);
                _saveRequested = false;
                await _saveService.DeleteAsync(linkedCancellation.Token);
            }
            finally
            {
                _ignoreBalanceChanges = false;
                _saveOperationLock.Release();
            }
        }

        public void Dispose()
        {
            Unsubscribe();
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
        }

        private void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            _economyService.BalanceChanged += OnBalanceChanged;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _economyService.BalanceChanged -= OnBalanceChanged;
            _isSubscribed = false;
        }

        private void OnBalanceChanged(int balance)
        {
            if (_ignoreBalanceChanges)
            {
                return;
            }

            _saveRequested = true;
            if (!_isSaving)
            {
                SaveLatestProgressAsync().Forget();
            }
        }

        private async UniTask SaveLatestProgressAsync()
        {
            _isSaving = true;
            try
            {
                do
                {
                    _saveRequested = false;
                    await _saveOperationLock.WaitAsync(_lifetimeCancellation.Token);
                    try
                    {
                        GameProgressData progress = new GameProgressData(_economyService.Balance);
                        await _saveService.SaveAsync(progress, _lifetimeCancellation.Token);
                    }
                    finally
                    {
                        _saveOperationLock.Release();
                    }
                }
                while (_saveRequested);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isSaving = false;
            }
        }
    }
}
