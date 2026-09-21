using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using ProjectSparks.Driving;
using UnityEngine;
using Zenject;
namespace ProjectSparks.Companions
{
    public sealed class TripSparkPresenter : IInitializable, IDisposable
    {
        private readonly PassengerSeat _seat;
        private readonly ICompanionService _selection;
        private readonly ISparkProgressService _sparks;
        private readonly IProgressService _progress;
        private readonly SparkVisualFactory _factory;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private GameObject _instance;
        private string _shownId;
        private bool _subscribed;
        public TripSparkPresenter(PassengerSeat seat, ICompanionService selection, ISparkProgressService sparks,
            IProgressService progress, SparkVisualFactory factory)
        { _seat = seat; _selection = selection; _sparks = sparks; _progress = progress; _factory = factory; }
        public void Initialize() => InitializeAsync().Forget();
        private async UniTask InitializeAsync()
        {
            try
            {
                await UniTask.WaitUntil(() => _progress.IsLoaded, cancellationToken: _lifetime.Token);
                _selection.SelectedCompanionChanged += Refresh;
                _subscribed = true;
                Refresh(_selection.SelectedCompanion);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        private void Refresh(string id)
        {
            if (id == _shownId && _instance != null)
            { _factory.Apply(_instance, _sparks.GetProgress(id)); return; }
            if (_instance != null) { _instance.SetActive(false); UnityEngine.Object.Destroy(_instance); _instance = null; }
            _shownId = id;
            if (!_sparks.Contains(id)) return;
            _instance = _factory.Create(_sparks.GetProgress(id), _seat.Point);
        }
        public void Dispose()
        {
            _lifetime.Cancel(); _lifetime.Dispose();
            if (_subscribed) _selection.SelectedCompanionChanged -= Refresh;
            if (_instance != null) UnityEngine.Object.Destroy(_instance);
        }
    }
}
