using System;
using System.Collections.Generic;
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
        private readonly FruitCatalog _catalog;
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
            IResourceInventoryService resourceInventoryService,
            FruitCatalog catalog)
        {
            _saveService = saveService;
            _economyService = economyService;
            _companionService = companionService;
            _sparkProgressService = sparkProgressService;
            _resourceInventoryService = resourceInventoryService;
            _catalog = catalog;
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
                    _starterFruitsPending = progress == null ||
                        progress.Version != GameProgressData.CurrentVersion;
                    if (_starterFruitsPending) progress = null;
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
                _resourceInventoryService.RestoreFruits(progress.GetFruitAmounts(_catalog));
                IsLoaded = true;
                Subscribe();
            }
            finally
            {
                _loadOperationLock.Release();
            }
        }

        public bool TryInitializeStarterFruits()
        {
            if (!IsLoaded || !_starterFruitsPending)
            {
                return false;
            }

            _starterFruitsPending = false;
            _ignoreProgressChanges = true;
            try
            {
                Dictionary<string, int> amounts = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < _catalog.Count; i++)
                {
                    FruitCatalog.Entry entry = _catalog.GetEntry(i);
                    amounts.Add(entry.FruitId, entry.StartingAmount);
                }
                _resourceInventoryService.RestoreFruits(amounts);
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
                        MergeFruitAmounts(result),
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
                _resourceInventoryService.RestoreFruits(_pendingTripCommit.GetFruitAmounts(_catalog));
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
                            _resourceInventoryService.FruitAmounts,
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

        private IReadOnlyDictionary<string, int> MergeFruitAmounts(TripResult result)
        {
            Dictionary<string, int> amounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> pair in _resourceInventoryService.FruitAmounts)
                amounts.Add(pair.Key, pair.Value);
            foreach (KeyValuePair<string, int> pair in result.FruitAmounts)
            {
                amounts.TryGetValue(pair.Key, out int current);
                amounts[pair.Key] = checked(current + pair.Value);
            }
            return amounts;
        }
    }
}
