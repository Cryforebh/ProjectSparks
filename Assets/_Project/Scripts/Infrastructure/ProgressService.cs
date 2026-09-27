using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Collectibles;
using ProjectSparks.Companions;
using ProjectSparks.Core;
using ProjectSparks.Missions;
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
        private readonly SemaphoreSlim _loadOperationLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _saveOperationLock = new SemaphoreSlim(1, 1);

        private bool _isSubscribed;
        private bool _isSaving;
        private bool _saveRequested;
        private bool _ignoreProgressChanges;
        private bool _starterFruitsPending;
        private string _lastCommittedTripId;
        private GameProgressData _pendingTripCommit;

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
        public event Action ResetCompleted;

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

            await _loadOperationLock.WaitAsync(linkedCancellation.Token);
            try
            {
                if (IsLoaded)
                {
                    return;
                }

                GameProgressData progress;
                try
                {
                    progress = await _saveService.LoadAsync(linkedCancellation.Token);
                    _starterFruitsPending = progress == null;
                    progress ??= new GameProgressData(0, string.Empty, 0);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    _starterFruitsPending = false;
                    progress = new GameProgressData(0, string.Empty, 0);
                }

                _lastCommittedTripId = progress.LastCommittedTripId;
                _economyService.Restore(progress.Balance);
                _sparkProgressService.Reset();
                if (progress.Sparks != null)
                {
                    foreach (SparkProgressData spark in progress.Sparks) _sparkProgressService.Restore(spark);
                }
                _companionService.Restore(progress.SelectedCompanion);
                _resourceInventoryService.RestoreCapsules(progress.CapsuleCount);
                _resourceInventoryService.RestoreFruits(
                    progress.SwimFruitCount,
                    progress.FlyFruitCount,
                    progress.RunFruitCount,
                    progress.PowerFruitCount,
                    progress.StaminaFruitCount);
                IsLoaded = true;
                Subscribe();
            }
            finally
            {
                _loadOperationLock.Release();
            }
        }

        public bool TryInitializeStarterFruits(
            int swim, int fly, int run, int power, int stamina)
        {
            if (!IsLoaded || !_starterFruitsPending)
            {
                return false;
            }

            _starterFruitsPending = false;
            _ignoreProgressChanges = true;
            try
            {
                _resourceInventoryService.RestoreFruits(swim, fly, run, power, stamina);
            }
            finally
            {
                _ignoreProgressChanges = false;
            }
            RequestSave();
            return true;
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
                _companionService.Restore(string.Empty);
                _sparkProgressService.Reset();
                _resourceInventoryService.Reset();
                _starterFruitsPending = true;
                _lastCommittedTripId = null;
                _pendingTripCommit = null;
                _saveRequested = false;
                await _saveService.DeleteAsync(linkedCancellation.Token);
            }
            finally
            {
                _ignoreProgressChanges = false;
                _saveOperationLock.Release();
            }

            ResetCompleted?.Invoke();
        }

        public void Dispose()
        {
            Unsubscribe();
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
            _loadOperationLock.Dispose();
        }

        public async UniTask CommitTripAsync(TripResult result, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            await LoadAsync(cancellationToken);
            using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _lifetimeCancellation.Token);
            await _saveOperationLock.WaitAsync(linkedCancellation.Token);
            try
            {
                if (string.Equals(_lastCommittedTripId, result.SessionId, StringComparison.Ordinal))
                {
                    return;
                }

                if (_pendingTripCommit == null)
                {
                    GameProgressData progress = new GameProgressData(
                        checked(_economyService.Balance + result.Coins),
                        _companionService.SelectedCompanion,
                        checked(_resourceInventoryService.CapsuleCount + result.CapsuleCount),
                        checked(_resourceInventoryService.GetFruitAmount(FruitType.Swim) + result.GetFruitAmount(FruitType.Swim)),
                        checked(_resourceInventoryService.GetFruitAmount(FruitType.Fly) + result.GetFruitAmount(FruitType.Fly)),
                        checked(_resourceInventoryService.GetFruitAmount(FruitType.Run) + result.GetFruitAmount(FruitType.Run)),
                        checked(_resourceInventoryService.GetFruitAmount(FruitType.Power) + result.GetFruitAmount(FruitType.Power)),
                        checked(_resourceInventoryService.GetFruitAmount(FruitType.Stamina) + result.GetFruitAmount(FruitType.Stamina)),
                        result.SessionId, _sparkProgressService.GetAll());
                    await _saveService.SaveAsync(progress, linkedCancellation.Token);
                    _pendingTripCommit = progress;
                }

                if (_pendingTripCommit.LastCommittedTripId != result.SessionId)
                {
                    throw new InvalidOperationException("Нужно завершить применение предыдущего сохраненного результата.");
                }

                _ignoreProgressChanges = true;
                _economyService.Restore(_pendingTripCommit.Balance);
                _resourceInventoryService.RestoreCapsules(_pendingTripCommit.CapsuleCount);
                _resourceInventoryService.RestoreFruits(
                    _pendingTripCommit.SwimFruitCount, _pendingTripCommit.FlyFruitCount,
                    _pendingTripCommit.RunFruitCount, _pendingTripCommit.PowerFruitCount,
                    _pendingTripCommit.StaminaFruitCount);
                _lastCommittedTripId = result.SessionId;
                _pendingTripCommit = null;
            }
            finally
            {
                _ignoreProgressChanges = false;
                _saveOperationLock.Release();
            }
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

        private void OnSelectedCompanionChanged(string sparkId)
        {
            RequestSave();
        }

        private void OnResourceAmountChanged(ResourceType resourceType, int amount)
        {
            RequestSave();
        }

        private void OnSparkChanged(string sparkId)
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
                        GameProgressData progress = _pendingTripCommit ?? new GameProgressData(
                            _economyService.Balance,
                            _companionService.SelectedCompanion,
                            _resourceInventoryService.CapsuleCount,
                            _resourceInventoryService.GetFruitAmount(FruitType.Swim),
                            _resourceInventoryService.GetFruitAmount(FruitType.Fly),
                            _resourceInventoryService.GetFruitAmount(FruitType.Run),
                            _resourceInventoryService.GetFruitAmount(FruitType.Power),
                            _resourceInventoryService.GetFruitAmount(FruitType.Stamina),
                            _lastCommittedTripId, _sparkProgressService.GetAll());
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
