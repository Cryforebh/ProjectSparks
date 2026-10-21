using System;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Missions
{
    /// <summary>
    /// Превращает состояние всей поездки в однократное событие завершения.
    /// Не управляет UI, звуком, наградами и переходами между сценами.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TripCompletionNotifier : MonoBehaviour
    {
        private ITripSessionService _session;
        private bool _subscribed;

        public event Action Completed;
        public event Action Cleared;

        public bool IsComplete { get; private set; }

        [Inject]
        private void Construct(ITripSessionService session)
        {
            _session = session;
            Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || !isActiveAndEnabled || _session == null)
                return;

            _session.Changed += Refresh;
            _subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _session == null)
                return;

            _session.Changed -= Refresh;
            _subscribed = false;
        }

        private void Refresh()
        {
            bool complete = _session.State == TripSessionState.Succeeded;
            if (complete == IsComplete)
                return;

            IsComplete = complete;
            if (complete)
                Completed?.Invoke();
            else
                Cleared?.Invoke();
        }
    }
}
