using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Collectibles;
using ProjectSparks.Companions;
using ProjectSparks.Core;
using ProjectSparks.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class SparkInteractionView : MonoBehaviour
    {
        [Header("Панель")]
        [Tooltip("Панель осмотра Искры. Компонент SparkInteractionView должен находиться вне этой панели.")]
        [SerializeField] private GameObject _panel;
        [Tooltip("Панель изменения Искры.")]
        [SerializeField] private GameObject _panelInteract;
        [Tooltip("Текст имени Искры.")]
        [SerializeField] private TMP_Text _nameText;
        [Tooltip("Текст описания способности Искры.")]
        [SerializeField] private TMP_Text _abilityText;
        [Tooltip("Текст состояния выбора Искры.")]
        [SerializeField] private TMP_Text _selectionStateText;
        [Header("Характеристики")]
        [Tooltip("Числовое значение характеристики плавания.")]
        [SerializeField] private TMP_Text _swimValueText;
        [Tooltip("Числовое значение характеристики полета.")]
        [SerializeField] private TMP_Text _flyValueText;
        [Tooltip("Числовое значение характеристики бега.")]
        [SerializeField] private TMP_Text _runValueText;
        [Tooltip("Числовое значение характеристики силы.")]
        [SerializeField] private TMP_Text _powerValueText;
        [Tooltip("Числовое значение характеристики выносливости.")]
        [SerializeField] private TMP_Text _staminaValueText;

        [Header("Предпросмотр кормления")]
        [Tooltip("Текст прибавки к плаванию.")]
        [SerializeField] private TMP_Text _swimIncreaseText;
        [Tooltip("Текст прибавки к полету.")]
        [SerializeField] private TMP_Text _flyIncreaseText;
        [Tooltip("Текст прибавки к бегу.")]
        [SerializeField] private TMP_Text _runIncreaseText;
        [Tooltip("Текст прибавки к силе.")]
        [SerializeField] private TMP_Text _powerIncreaseText;
        [Tooltip("Текст прибавки к выносливости.")]
        [SerializeField] private TMP_Text _staminaIncreaseText;

        [Header("Имя")]
        [Tooltip("Поле ввода нового имени Искры.")]
        [SerializeField] private TMP_InputField _nameInputField;
        [Tooltip("Кнопка подтверждения нового имени Искры.")]
        [SerializeField] private Button _renameButton;

        [Header("Действия")]
        [Tooltip("Кнопка выбора Искры активным компаньоном.")]
        [SerializeField] private Button _selectButton;
        [Tooltip("Кнопка возврата Искры на прежнее место.")]
        [SerializeField] private Button _putDownButton;
        [Tooltip("Кнопка подтверждения кормления Искры.")]
        [SerializeField] private Button _feedButton;

        private IPlayerHandsService _handsService;
        private IPlayerControlService _controlService;
        private ICompanionService _companionService;
        private ISparkProgressService _sparkProgressService;
        private ISparkFeedingService _feedingService;
        private IFruitWorldService _fruitWorldService;
        private IShelterInteractionRequestService _requestService;
        private IProgressService _progressService;
        private ISparkProfile _currentSpark;
        private IFruitItem _currentFruit;
        private TMP_Text _putDownButtonText;
        private bool _isReady;
        private bool _isFeedingMode;

        [Inject]
        private void Construct(
            IPlayerHandsService handsService,
            IPlayerControlService controlService,
            ICompanionService companionService,
            ISparkProgressService sparkProgressService,
            ISparkFeedingService feedingService,
            IFruitWorldService fruitWorldService,
            IShelterInteractionRequestService requestService,
            IProgressService progressService)
        {
            _handsService = handsService;
            _controlService = controlService;
            _companionService = companionService;
            _sparkProgressService = sparkProgressService;
            _feedingService = feedingService;
            _fruitWorldService = fruitWorldService;
            _requestService = requestService;
            _progressService = progressService;
        }

        private void Start()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError("В SparkInteractionView назначены не все ссылки.", this);
                enabled = false;
                return;
            }

            if (_panel == gameObject || _panelInteract == gameObject)
            {
                Debug.LogError("Панель SparkInteractionView должна быть дочерним объектом отдельного активного корня.", this);
                enabled = false;
                return;
            }

            _selectButton.onClick.AddListener(HandleSelectClicked);
            _putDownButton.onClick.AddListener(HandlePutDownClicked);
            _feedButton.onClick.AddListener(HandleFeedClicked);
            _renameButton.onClick.AddListener(HandleRenameClicked);
            _handsService.HeldItemChanged += HandleHeldItemChanged;
            _companionService.SelectedCompanionChanged += HandleSelectedCompanionChanged;
            _sparkProgressService.SparkChanged += HandleSparkChanged;
            _requestService.SparkFeedingRequested += HandleSparkFeedingRequested;

            _putDownButtonText = _putDownButton.GetComponentInChildren<TMP_Text>(true);
            ClosePanel();
            InitializeAsync().Forget();
        }

        private void OnDisable()
        {
            if (_handsService?.HeldItem is PhysicalFruitItem)
            {
                _fruitWorldService?.TryReturnHeldFruitToCrate();
            }
            else if (_handsService != null && _handsService.HasItem)
            {
                _handsService.TryRelease();
            }

            _controlService?.SetControlEnabled(true);
        }

        private void OnDestroy()
        {
            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveListener(HandleSelectClicked);
            }

            if (_putDownButton != null)
            {
                _putDownButton.onClick.RemoveListener(HandlePutDownClicked);
            }

            if (_renameButton != null)
            {
                _renameButton.onClick.RemoveListener(HandleRenameClicked);
            }

            if (_feedButton != null)
            {
                _feedButton.onClick.RemoveListener(HandleFeedClicked);
            }

            if (_handsService != null)
            {
                _handsService.HeldItemChanged -= HandleHeldItemChanged;
            }

            if (_companionService != null)
            {
                _companionService.SelectedCompanionChanged -= HandleSelectedCompanionChanged;
            }

            if (_sparkProgressService != null)
            {
                _sparkProgressService.SparkChanged -= HandleSparkChanged;
            }

            if (_requestService != null)
            {
                _requestService.SparkFeedingRequested -= HandleSparkFeedingRequested;
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
            }
        }

        private void HandleHeldItemChanged(IHoldableItem heldItem)
        {
            _currentSpark = heldItem as ISparkProfile;
            if (_currentSpark == null)
            {
                ClosePanel();
                return;
            }

            _currentFruit = null;
            _isFeedingMode = false;
            _abilityText.text = _currentSpark.AbilityDescription;
            OpenPanels();
            RefreshView();
        }

        private void HandleSparkFeedingRequested(
            ISparkProfile spark,
            IFruitItem fruit)
        {
            if (spark == null
                || fruit == null
                || !ReferenceEquals(_handsService.HeldItem, fruit))
            {
                return;
            }

            _currentSpark = spark;
            _currentFruit = fruit;
            _isFeedingMode = true;
            _abilityText.text = $"Кормление: {fruit.DisplayName}";
            OpenPanels();
            RefreshView();
        }

        private void HandleSelectedCompanionChanged(CompanionType companionType)
        {
            RefreshSelection();
        }

        private void HandleSparkChanged(CompanionType companionType)
        {
            if (_currentSpark != null
                && _currentSpark.CompanionType == companionType)
            {
                RefreshView();
            }
        }

        private void HandleSelectClicked()
        {
            if (_isReady && _currentSpark != null)
            {
                _companionService.Select(_currentSpark.CompanionType);
            }
        }

        private void HandlePutDownClicked()
        {
            if (_isFeedingMode)
            {
                ClosePanel();
                return;
            }

            _handsService.TryRelease();
        }

        private void HandleFeedClicked()
        {
            if (!_isReady
                || !_isFeedingMode
                || _currentSpark == null
                || _currentFruit == null)
            {
                return;
            }

            CompanionType companionType = _currentSpark.CompanionType;
            FruitType fruitType = _currentFruit.FruitType;
            if (!_feedingService.CanFeed(companionType, fruitType)
                || _currentSpark is not ISparkStateController stateController
                || !stateController.TryStartEating())
            {
                return;
            }

            if (!_fruitWorldService.TryConsumeHeldFruit()
                || !_feedingService.TryFeed(companionType, fruitType))
            {
                stateController.CompleteEating();
                return;
            }

            ClosePanel();
        }

        private void HandleRenameClicked()
        {
            if (_isReady && _currentSpark != null)
            {
                _sparkProgressService.Rename(
                    _currentSpark.CompanionType,
                    _nameInputField.text);
            }
        }

        private void RefreshView()
        {
            if (_currentSpark == null)
            {
                _selectButton.interactable = false;
                _renameButton.interactable = false;
                return;
            }

            SparkProgressData progress = _sparkProgressService.GetProgress(
                _currentSpark.CompanionType);
            _nameText.text = progress.DisplayName;
            _nameInputField.SetTextWithoutNotify(progress.DisplayName);
            _swimValueText.text = progress
                .GetStat(SparkStatType.Swim)
                .ToString();
            _flyValueText.text = progress
                .GetStat(SparkStatType.Fly)
                .ToString();
            _runValueText.text = progress
                .GetStat(SparkStatType.Run)
                .ToString();
            _powerValueText.text = progress
                .GetStat(SparkStatType.Power)
                .ToString();
            _staminaValueText.text = progress
                .GetStat(SparkStatType.Stamina)
                .ToString();

            SetFeedingMode(_isFeedingMode);
            if (_isFeedingMode && _currentFruit != null)
            {
                RefreshFeedingPreview();
            }
            else
            {
                _renameButton.interactable = _isReady;
                RefreshSelection();
            }
        }

        private void RefreshFeedingPreview()
        {
            SparkStatValues increase = _feedingService.GetIncrease(
                _currentFruit.FruitType);
            _swimIncreaseText.text = $"+{increase.Swim}";
            _flyIncreaseText.text = $"+{increase.Fly}";
            _runIncreaseText.text = $"+{increase.Run}";
            _powerIncreaseText.text = $"+{increase.Power}";
            _staminaIncreaseText.text = $"+{increase.Stamina}";
            _feedButton.interactable = _isReady
                && _feedingService.CanFeed(
                    _currentSpark.CompanionType,
                    _currentFruit.FruitType);
        }

        private void SetFeedingMode(bool isFeedingMode)
        {
            _selectionStateText.gameObject.SetActive(!isFeedingMode);
            _nameInputField.gameObject.SetActive(!isFeedingMode);
            _renameButton.gameObject.SetActive(!isFeedingMode);
            _selectButton.gameObject.SetActive(!isFeedingMode);
            _feedButton.gameObject.SetActive(isFeedingMode);
            _putDownButton.gameObject.SetActive(true);
            if (_putDownButtonText != null)
            {
                _putDownButtonText.text = isFeedingMode
                    ? "ОТМЕНА"
                    : "ОПУСТИТЬ";
            }

            SetIncreaseTextsActive(isFeedingMode);
        }

        private void SetIncreaseTextsActive(bool isActive)
        {
            _swimIncreaseText.gameObject.SetActive(isActive);
            _flyIncreaseText.gameObject.SetActive(isActive);
            _runIncreaseText.gameObject.SetActive(isActive);
            _powerIncreaseText.gameObject.SetActive(isActive);
            _staminaIncreaseText.gameObject.SetActive(isActive);
        }

        private void OpenPanels()
        {
            _panel.SetActive(true);
            _panelInteract.SetActive(true);
            _controlService.SetControlEnabled(false);
        }

        private void RefreshSelection()
        {
            if (_currentSpark == null)
            {
                _selectButton.interactable = false;
                return;
            }

            bool isSelected =
                _companionService.SelectedCompanion == _currentSpark.CompanionType;
            _selectionStateText.text = isSelected
                ? "Выбрана активным компаньоном"
                : "Можно выбрать активным компаньоном";
            _selectButton.interactable = _isReady && !isSelected;
        }

        private void ClosePanel()
        {
            _currentSpark = null;
            _currentFruit = null;
            _isFeedingMode = false;
            if (_panel != null)
            {
                _panel.SetActive(false);
                _panelInteract.SetActive(false);
            }

            _controlService?.SetControlEnabled(true);
        }

        private bool HasRequiredReferences()
        {
            return _panel != null
                && _panelInteract != null
                && _nameText != null
                && _abilityText != null
                && _selectionStateText != null
                && _swimValueText != null
                && _flyValueText != null
                && _runValueText != null
                && _powerValueText != null
                && _staminaValueText != null
                && _swimIncreaseText != null
                && _flyIncreaseText != null
                && _runIncreaseText != null
                && _powerIncreaseText != null
                && _staminaIncreaseText != null
                && _nameInputField != null
                && _renameButton != null
                && _selectButton != null
                && _putDownButton != null
                && _feedButton != null;
        }
    }
}
