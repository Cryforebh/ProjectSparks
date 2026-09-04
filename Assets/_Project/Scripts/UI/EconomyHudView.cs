using ProjectSparks.Core;
using TMPro;
using UnityEngine;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class EconomyHudView : MonoBehaviour
    {
        [Tooltip("Текст текущего количества монет.")]
        [SerializeField] private TMP_Text _balanceText;

        private IEconomyService _economyService;
        private bool _isSubscribed;

        [Inject]
        public void Construct(IEconomyService economyService)
        {
            _economyService = economyService;
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
            if (_isSubscribed || _economyService == null)
            {
                return;
            }

            _economyService.BalanceChanged += Refresh;
            _isSubscribed = true;
            Refresh(_economyService.Balance);
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _economyService.BalanceChanged -= Refresh;
            _isSubscribed = false;
        }

        private void Refresh(int balance)
        {
            if (_balanceText != null)
            {
                _balanceText.text = $"Монеты: {balance}";
            }
        }
    }
}
