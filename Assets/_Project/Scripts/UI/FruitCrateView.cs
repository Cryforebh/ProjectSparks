using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Collectibles;
using ProjectSparks.Core;
using ProjectSparks.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class FruitCrateView : MonoBehaviour
    {
        [Header("Панель")]
        [Tooltip("Панель выбора фруктов.")]
        [SerializeField] private GameObject _panel;

        [Header("Кнопки фруктов")]
        [Tooltip("Кнопка выбора фрукта плавания.")]
        [SerializeField] private Button _swimButton;
        [Tooltip("Кнопка выбора фрукта полета.")]
        [SerializeField] private Button _flyButton;
        [Tooltip("Кнопка выбора фрукта бега.")]
        [SerializeField] private Button _runButton;
        [Tooltip("Кнопка выбора фрукта силы.")]
        [SerializeField] private Button _powerButton;
        [Tooltip("Кнопка выбора фрукта выносливости.")]
        [SerializeField] private Button _staminaButton;
        [Tooltip("Кнопка закрытия ящика без выбора фрукта.")]
        [SerializeField] private Button _cancelButton;

        [Header("Количество фруктов")]
        [Tooltip("Текст количества фруктов плавания.")]
        [SerializeField] private TMP_Text _swimCountText;
        [Tooltip("Текст количества фруктов полета.")]
        [SerializeField] private TMP_Text _flyCountText;
        [Tooltip("Текст количества фруктов бега.")]
        [SerializeField] private TMP_Text _runCountText;
        [Tooltip("Текст количества фруктов силы.")]
        [SerializeField] private TMP_Text _powerCountText;
        [Tooltip("Текст количества фруктов выносливости.")]
        [SerializeField] private TMP_Text _staminaCountText;

        private IResourceInventoryService _inventoryService;
        private IPlayerHandsService _handsService;
        private IPlayerControlService _controlService;
        private IFruitWorldService _fruitWorldService;
        private IShelterInteractionRequestService _requestService;
        private IProgressService _progressService;
        private bool _isReady;

        [Inject]
        private void Construct(
            IResourceInventoryService inventoryService,
            IPlayerHandsService handsService,
            IPlayerControlService controlService,
            IFruitWorldService fruitWorldService,
            IShelterInteractionRequestService requestService,
            IProgressService progressService)
        {
            _inventoryService = inventoryService;
            _handsService = handsService;
            _controlService = controlService;
            _fruitWorldService = fruitWorldService;
            _requestService = requestService;
            _progressService = progressService;
        }

        private void Start()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError("В FruitCrateView назначены не все ссылки.", this);
                enabled = false;
                return;
            }

            if (_panel == gameObject)
            {
                Debug.LogError("FruitCrateView должен находиться вне отключаемой панели.", this);
                enabled = false;
                return;
            }

            _swimButton.onClick.AddListener(HandleSwimClicked);
            _flyButton.onClick.AddListener(HandleFlyClicked);
            _runButton.onClick.AddListener(HandleRunClicked);
            _powerButton.onClick.AddListener(HandlePowerClicked);
            _staminaButton.onClick.AddListener(HandleStaminaClicked);
            _cancelButton.onClick.AddListener(ClosePanel);
            _inventoryService.FruitAmountChanged += HandleFruitAmountChanged;
            _requestService.FruitCrateRequested += OpenPanel;

            _panel.SetActive(false);
            InitializeAsync().Forget();
        }

        private void OnDisable()
        {
            if (_panel != null && _panel.activeSelf)
            {
                ClosePanel();
            }
        }

        private void OnDestroy()
        {
            if (_swimButton != null)
            {
                _swimButton.onClick.RemoveListener(HandleSwimClicked);
            }

            if (_flyButton != null)
            {
                _flyButton.onClick.RemoveListener(HandleFlyClicked);
            }

            if (_runButton != null)
            {
                _runButton.onClick.RemoveListener(HandleRunClicked);
            }

            if (_powerButton != null)
            {
                _powerButton.onClick.RemoveListener(HandlePowerClicked);
            }

            if (_staminaButton != null)
            {
                _staminaButton.onClick.RemoveListener(HandleStaminaClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(ClosePanel);
            }

            if (_inventoryService != null)
            {
                _inventoryService.FruitAmountChanged -= HandleFruitAmountChanged;
            }

            if (_requestService != null)
            {
                _requestService.FruitCrateRequested -= OpenPanel;
            }
        }

        private async UniTask InitializeAsync()
        {
            try
            {
                await _progressService.LoadAsync(this.GetCancellationTokenOnDestroy());
                _isReady = true;
                RefreshAll();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void OpenPanel()
        {
            if (_handsService.HasItem)
            {
                return;
            }

            _panel.SetActive(true);
            _controlService.SetControlEnabled(false);
            RefreshAll();
        }

        private void ClosePanel()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            _controlService?.SetControlEnabled(true);
        }

        private void HandleSwimClicked()
        {
            TryTakeFruit(FruitType.Swim);
        }

        private void HandleFlyClicked()
        {
            TryTakeFruit(FruitType.Fly);
        }

        private void HandleRunClicked()
        {
            TryTakeFruit(FruitType.Run);
        }

        private void HandlePowerClicked()
        {
            TryTakeFruit(FruitType.Power);
        }

        private void HandleStaminaClicked()
        {
            TryTakeFruit(FruitType.Stamina);
        }

        private void TryTakeFruit(FruitType fruitType)
        {
            if (!_isReady
                || _inventoryService.GetFruitAmount(fruitType) <= 0
                || _handsService.HasItem)
            {
                return;
            }

            if (_fruitWorldService.TryTakeFromCrate(fruitType))
            {
                ClosePanel();
            }
        }

        private void HandleFruitAmountChanged(FruitType fruitType, int amount)
        {
            RefreshFruit(fruitType);
        }

        private void RefreshAll()
        {
            RefreshFruit(FruitType.Swim);
            RefreshFruit(FruitType.Fly);
            RefreshFruit(FruitType.Run);
            RefreshFruit(FruitType.Power);
            RefreshFruit(FruitType.Stamina);
        }

        private void RefreshFruit(FruitType fruitType)
        {
            int amount = _inventoryService.GetFruitAmount(fruitType);
            GetCountText(fruitType).text = amount.ToString();
            GetButton(fruitType).interactable = _isReady && amount > 0;
        }

        private TMP_Text GetCountText(FruitType fruitType)
        {
            return fruitType switch
            {
                FruitType.Swim => _swimCountText,
                FruitType.Fly => _flyCountText,
                FruitType.Run => _runCountText,
                FruitType.Power => _powerCountText,
                FruitType.Stamina => _staminaCountText,
                _ => throw new ArgumentOutOfRangeException(nameof(fruitType))
            };
        }

        private Button GetButton(FruitType fruitType)
        {
            return fruitType switch
            {
                FruitType.Swim => _swimButton,
                FruitType.Fly => _flyButton,
                FruitType.Run => _runButton,
                FruitType.Power => _powerButton,
                FruitType.Stamina => _staminaButton,
                _ => throw new ArgumentOutOfRangeException(nameof(fruitType))
            };
        }

        private bool HasRequiredReferences()
        {
            return _panel != null
                && _swimButton != null
                && _flyButton != null
                && _runButton != null
                && _powerButton != null
                && _staminaButton != null
                && _cancelButton != null
                && _swimCountText != null
                && _flyCountText != null
                && _runCountText != null
                && _powerCountText != null
                && _staminaCountText != null;
        }
    }
}
