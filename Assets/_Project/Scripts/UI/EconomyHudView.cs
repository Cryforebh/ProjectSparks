using ProjectSparks.Missions;
using TMPro;
using UnityEngine;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class EconomyHudView : MonoBehaviour
    {
        [Tooltip("Текст монет, полученных за текущую поездку.")]
        [SerializeField] private TMP_Text _balanceText;

        private ITripSessionService _tripSession;
        private bool _isSubscribed;

        [Inject]
        public void Construct(ITripSessionService tripSession)
        {
            _tripSession = tripSession;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed || _tripSession == null)
            {
                return;
            }

            _tripSession.Changed += Refresh;
            _isSubscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _tripSession.Changed -= Refresh;
            _isSubscribed = false;
        }

        private void Refresh()
        {
            if (_balanceText != null)
            {
                _balanceText.text = $"Монеты: {_tripSession.Coins}";
            }
        }
    }
}
