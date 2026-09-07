using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Collectibles;
using ProjectSparks.Companions;
using ProjectSparks.Core;
using UnityEngine;

namespace ProjectSparks.Infrastructure
{
    public sealed class ProgressService : IProgressService, IDisposable
    {
        private readonly ISaveService _saveService;
        private readonly IEconomyService _economyService;
        private readonly ICompanionService _companionService;
        private readonly ISparkProgressService _sparkProgressService;
        private readonly IResourceInventoryService _resourceInventoryService;
        private readonly CancellationTokenSource _lifetimeCancellation = new CancellationTokenSource();
        private readonly SemaphoreSlim _saveOperationLock = new SemaphoreSlim(1, 1);

        private bool _isSubscribed;
        private bool _isSaving;
        private bool _saveRequested;
        private bool _ignoreProgressChanges;

        public ProgressService(
            ISaveService saveService,
            IEconomyService economyService,
            ICompanionService companionService,
            ISparkProgressService sparkProgressService,
            IResourceInventoryService resourceInventoryService)
        {
            _saveService = saveService;
            _economyService = economyService;
            _companionService = companionService;
            _sparkProgressService = sparkProgressService;
            _resourceInventoryService = resourceInventoryService;
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
                progress = new GameProgressData(0, CompanionType.None, 0, 0);
            }

            _economyService.Restore(progress.Balance);
            _companionService.Restore(progress.SelectedCompanion);
            _sparkProgressService.Reset();
            _sparkProgressService.Restore(progress.CollectorSpark);
            _sparkProgressService.Restore(progress.MechanicSpark);
            _resourceInventoryService.Restore(
                progress.FruitCount,
                progress.CapsuleCount);
            _resourceInventoryService.RestoreFruits(
                progress.SwimFruitCount,
                progress.FlyFruitCount,
                progress.RunFruitCount,
                progress.PowerFruitCount,
                progress.StaminaFruitCount);
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
                _ignoreProgressChanges = true;
                _economyService.Restore(0);
                _companionService.Restore(CompanionType.None);
                _sparkProgressService.Reset();
                _resourceInventoryService.Reset();
                _saveRequested = false;
                await _saveService.DeleteAsync(linkedCancellation.Token);
            }
            finally
            {
                _ignoreProgressChanges = false;
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
            _companionService.SelectedCompanionChanged += OnSelectedCompanionChanged;
            _sparkProgressService.SparkChanged += OnSparkChanged;
            _resourceInventoryService.AmountChanged += OnResourceAmountChanged;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _economyService.BalanceChanged -= OnBalanceChanged;
            _companionService.SelectedCompanionChanged -= OnSelectedCompanionChanged;
            _sparkProgressService.SparkChanged -= OnSparkChanged;
            _resourceInventoryService.AmountChanged -= OnResourceAmountChanged;
            _isSubscribed = false;
        }

        private void OnBalanceChanged(int balance)
        {
            RequestSave();
        }

        private void OnSelectedCompanionChanged(CompanionType companionType)
        {
            RequestSave();
        }

        private void OnResourceAmountChanged(ResourceType resourceType, int amount)
        {
            RequestSave();
        }

        private void OnSparkChanged(CompanionType companionType)
        {
            RequestSave();
        }

        private void RequestSave()
        {
            if (_ignoreProgressChanges)
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
                        GameProgressData progress = new GameProgressData(
                            _economyService.Balance,
                            _companionService.SelectedCompanion,
                            _resourceInventoryService.FruitCount,
                            _resourceInventoryService.CapsuleCount,
                            _sparkProgressService.GetProgress(CompanionType.Collector),
                            _sparkProgressService.GetProgress(CompanionType.Mechanic),
                            _resourceInventoryService.GetFruitAmount(FruitType.Swim),
                            _resourceInventoryService.GetFruitAmount(FruitType.Fly),
                            _resourceInventoryService.GetFruitAmount(FruitType.Run),
                            _resourceInventoryService.GetFruitAmount(FruitType.Power),
                            _resourceInventoryService.GetFruitAmount(FruitType.Stamina));
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
