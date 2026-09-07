using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Collectibles;
using ProjectSparks.Companions;
using ProjectSparks.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class ShelterView : MonoBehaviour
    {
        [Header("Выбор компаньона")]
        [Tooltip("Кнопка выбора Собирателя.")]
        [SerializeField] private Button _collectorButton;
        [Tooltip("Кнопка выбора Механика.")]
        [SerializeField] private Button _mechanicButton;
        [Tooltip("Текст текущего выбранного компаньона.")]
        [SerializeField] private TMP_Text _selectedCompanionText;

        [Header("Ресурсы")]
        [Tooltip("Текст количества ресурсов в убежище.")]
        [SerializeField] private TMP_Text _resourceSummaryText;

        [Header("Навигация")]
        [Tooltip("Кнопка начала следующей поездки.")]
        [SerializeField] private Button _startTripButton;
        [Tooltip("Кнопка возврата в главное меню.")]
        [SerializeField] private Button _mainMenuButton;
        [Tooltip("Имя игровой сцены следующей поездки.")]
        [SerializeField] private string _gameSceneName = "DrivingSandbox";
        [Tooltip("Имя сцены главного меню.")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        private ICompanionService _companionService;
        private IResourceInventoryService _resourceInventoryService;
        private IProgressService _progressService;
        private ISceneLoader _sceneLoader;
        private bool _isLoadingScene;
        private bool _isReady;

        [Inject]
        private void Construct(
            ICompanionService companionService,
            IResourceInventoryService resourceInventoryService,
            IProgressService progressService,
            ISceneLoader sceneLoader)
        {
            _companionService = companionService;
            _resourceInventoryService = resourceInventoryService;
            _progressService = progressService;
            _sceneLoader = sceneLoader;
        }

        private void Start()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError("В ShelterView назначены не все ссылки.", this);
                enabled = false;
                return;
            }

            _collectorButton.onClick.AddListener(HandleCollectorClicked);
            _mechanicButton.onClick.AddListener(HandleMechanicClicked);
            _startTripButton.onClick.AddListener(HandleStartTripClicked);
            _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
            _companionService.SelectedCompanionChanged += HandleSelectedCompanionChanged;
            _resourceInventoryService.AmountChanged += HandleResourceAmountChanged;

            SetAllButtonsInteractable(false);
            InitializeAsync().Forget();
        }

        private void OnDestroy()
        {
            if (_collectorButton != null)
            {
                _collectorButton.onClick.RemoveListener(HandleCollectorClicked);
            }

            if (_mechanicButton != null)
            {
                _mechanicButton.onClick.RemoveListener(HandleMechanicClicked);
            }

            if (_startTripButton != null)
            {
                _startTripButton.onClick.RemoveListener(HandleStartTripClicked);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
            }

            if (_companionService != null)
            {
                _companionService.SelectedCompanionChanged -= HandleSelectedCompanionChanged;
            }

            if (_resourceInventoryService != null)
            {
                _resourceInventoryService.AmountChanged -= HandleResourceAmountChanged;
            }
        }

        private async UniTask InitializeAsync()
        {
            try
            {
                await _progressService.LoadAsync(this.GetCancellationTokenOnDestroy());
                _isReady = true;
                RefreshView();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                _mainMenuButton.interactable = true;
            }
        }

        private void HandleCollectorClicked()
        {
            if (_isReady)
            {
                _companionService.Select(CompanionType.Collector);
            }
        }

        private void HandleMechanicClicked()
        {
            if (_isReady)
            {
                _companionService.Select(CompanionType.Mechanic);
            }
        }

        private void HandleStartTripClicked()
        {
            if (_isReady && _companionService.SelectedCompanion != CompanionType.None)
            {
                LoadSceneAsync(_gameSceneName).Forget();
            }
        }

        private void HandleMainMenuClicked()
        {
            LoadSceneAsync(_mainMenuSceneName).Forget();
        }

        private void HandleSelectedCompanionChanged(CompanionType companionType)
        {
            RefreshCompanionSelection();
        }

        private void HandleResourceAmountChanged(ResourceType resourceType, int amount)
        {
            RefreshResourceSummary();
        }

        private async UniTask LoadSceneAsync(string sceneName)
        {
            if (_isLoadingScene)
            {
                return;
            }

            _isLoadingScene = true;
            SetAllButtonsInteractable(false);

            try
            {
                await _sceneLoader.LoadAsync(
                    sceneName,
                    this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                _isLoadingScene = false;
                RefreshButtonStates();
            }
        }

        private void RefreshView()
        {
            RefreshCompanionSelection();
            RefreshResourceSummary();
        }

        private void RefreshCompanionSelection()
        {
            CompanionType selectedCompanion = _companionService.SelectedCompanion;
            _selectedCompanionText.text = selectedCompanion switch
            {
                CompanionType.Collector => "Выбран: Собиратель",
                CompanionType.Mechanic => "Выбран: Механик",
                _ => "Выберите компаньона"
            };

            RefreshButtonStates();
        }

        private void RefreshResourceSummary()
        {
            _resourceSummaryText.text =
                $"Фрукты: {_resourceInventoryService.FruitCount}    "
                + $"Капсулы: {_resourceInventoryService.CapsuleCount}";
        }

        private void RefreshButtonStates()
        {
            if (!_isReady || _isLoadingScene)
            {
                SetAllButtonsInteractable(false);
                return;
            }

            CompanionType selectedCompanion = _companionService.SelectedCompanion;
            _collectorButton.interactable = selectedCompanion != CompanionType.Collector;
            _mechanicButton.interactable = selectedCompanion != CompanionType.Mechanic;
            _startTripButton.interactable = selectedCompanion != CompanionType.None;
            _mainMenuButton.interactable = true;
        }

        private void SetAllButtonsInteractable(bool isInteractable)
        {
            _collectorButton.interactable = isInteractable;
            _mechanicButton.interactable = isInteractable;
            _startTripButton.interactable = isInteractable;
            _mainMenuButton.interactable = isInteractable;
        }

        private bool HasRequiredReferences()
        {
            return _collectorButton != null
                && _mechanicButton != null
                && _selectedCompanionText != null
                && _resourceSummaryText != null
                && _startTripButton != null
                && _mainMenuButton != null;
        }
    }
}
