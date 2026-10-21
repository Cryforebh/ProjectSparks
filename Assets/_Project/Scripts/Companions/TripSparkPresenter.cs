using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using ProjectSparks.Driving;
using UnityEngine;
using Zenject;
namespace ProjectSparks.Companions
{
    public sealed class TripSparkPresenter : IInitializable, ITickable, IDisposable
    {
        private readonly PassengerSeat _seat;
        private readonly ICompanionService _selection;
        private readonly ISparkProgressService _sparks;
        private readonly IProgressService _progress;
        private readonly SparkVisualFactory _factory;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private GameObject _instance;
        private SparkAnimatorView _animatorView;
        private SparkPassengerSittingProfile _sittingProfile;
        private SparkSittingProfile.Variant _sittingVariant;
        private float _sittingEndTime;
        private bool _isPlayful;
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
            _animatorView = null;
            _sittingVariant = null;
            _isPlayful = false;
            _shownId = id;
            if (!_sparks.Contains(id)) return;
            _instance = _factory.Create(_sparks.GetProgress(id), _seat.Point);
            _animatorView = _instance.GetComponent<SparkAnimatorView>();
            _sittingProfile = _seat.SittingProfile;
            if (_animatorView == null || _sittingProfile == null ||
                _sittingProfile.Resting == null || _sittingProfile.Playful == null)
            {
                Debug.LogError("Для пассажира нужны SparkAnimatorView и профиль с двумя вариантами сидения.", _seat);
                _animatorView = null;
                return;
            }

            _animatorView.SetPassengerMode();
        }

        public void Tick()
        {
            if (_animatorView == null || !_animatorView.IsInitialized || Time.timeScale <= 0f)
                return;

            if (_sittingVariant == null)
            {
                PlaySitting(true);
                return;
            }

            bool finished = _sittingVariant.FinishOnClipEnd
                ? _animatorView.IsRestClipComplete()
                : Time.time >= _sittingEndTime;
            if (!finished)
                return;

            _isPlayful = !_isPlayful;
            PlaySitting(false);
        }

        private void PlaySitting(bool immediately)
        {
            SparkSittingProfile.Variant variant = _isPlayful ? _sittingProfile.Playful : _sittingProfile.Resting;
            _sittingEndTime = variant.FinishOnClipEnd
                ? float.PositiveInfinity
                : Time.time + variant.ChooseDuration();
            if (!_animatorView.PlaySitting(variant, _sittingEndTime, immediately))
            {
                _animatorView = null;
                return;
            }

            _sittingVariant = variant;
        }
        public void Dispose()
        {
            _lifetime.Cancel(); _lifetime.Dispose();
            if (_subscribed) _selection.SelectedCompanionChanged -= Refresh;
            if (_instance != null) UnityEngine.Object.Destroy(_instance);
        }
    }
}
