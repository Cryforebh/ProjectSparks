using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ProjectSparks.Collectibles;
using ProjectSparks.Core;
using ProjectSparks.Player;
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
        [Tooltip("Префаб одной кнопки фрукта с FruitCrateEntryView.")]
        [SerializeField] private FruitCrateEntryView _entryTemplate;
        [Tooltip("Кнопка закрытия ящика без выбора фрукта.")]
        [SerializeField] private Button _cancelButton;

        private readonly Dictionary<string, FruitCrateEntryView> _entries =
            new Dictionary<string, FruitCrateEntryView>(StringComparer.Ordinal);

        private FruitCatalog _catalog;
        private IResourceInventoryService _inventoryService;
        private IPlayerHandsService _handsService;
        private IPlayerControlService _controlService;
        private IFruitWorldService _fruitWorldService;
        private IShelterInteractionRequestService _requestService;
        private IProgressService _progressService;
        private bool _isReady;

        [Inject]
        private void Construct(
            FruitCatalog catalog,
            IResourceInventoryService inventoryService,
            IPlayerHandsService handsService,
            IPlayerControlService controlService,
            IFruitWorldService fruitWorldService,
            IShelterInteractionRequestService requestService,
            IProgressService progressService)
        {
            _catalog = catalog;
            _inventoryService = inventoryService;
            _handsService = handsService;
            _controlService = controlService;
            _fruitWorldService = fruitWorldService;
            _requestService = requestService;
            _progressService = progressService;
        }

        private void Start()
        {
            if (_panel == null || _panel == gameObject || _entryTemplate == null ||
                _cancelButton == null || _catalog == null)
            {
                Debug.LogError("FruitCrateView: проверьте панель, шаблон, кнопку закрытия и каталог.", this);
                enabled = false;
                return;
            }
            if (!_catalog.TryValidate(out string error))
            {
                Debug.LogError($"FruitCrateView: {error}", this);
                enabled = false;
                return;
            }

            BuildEntries();
            _cancelButton.onClick.AddListener(ClosePanel);
            _inventoryService.FruitAmountChanged += HandleFruitAmountChanged;
            _requestService.FruitCrateRequested += OpenPanel;
            _panel.SetActive(false);
            InitializeAsync().Forget();
        }

        private void BuildEntries()
        {
            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            GameObject list = new GameObject("FruitList", typeof(RectTransform),
                typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            list.transform.SetParent(panelRect, false);
            RectTransform listRect = (RectTransform)list.transform;
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.offsetMin = new Vector2(12f, 112f);
            listRect.offsetMax = new Vector2(-12f, -85f);
            Image background = list.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0f);

            GameObject content = new GameObject("FruitEntries", typeof(RectTransform),
                typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(listRect, false);
            RectTransform contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150f, 150f);
            grid.spacing = new Vector2(12f, 12f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = list.GetComponent<ScrollRect>();
            scroll.viewport = listRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            for (int i = 0; i < _catalog.Count; i++)
            {
                FruitCatalog.Entry entry = _catalog.GetEntry(i);
                FruitCrateEntryView view = Instantiate(_entryTemplate, contentRect);
                view.gameObject.SetActive(true);
                view.Bind(entry, TryTakeFruit);
                _entries.Add(entry.FruitId, view);
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
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        private void OpenPanel()
        {
            if (_handsService.HasItem) return;
            _panel.SetActive(true);
            _controlService.SetControlEnabled(false);
            RefreshAll();
        }

        private void ClosePanel()
        {
            if (_panel != null) _panel.SetActive(false);
            _controlService?.SetControlEnabled(true);
        }

        private void TryTakeFruit(string fruitId)
        {
            if (!_isReady || _inventoryService.GetFruitAmount(fruitId) <= 0 ||
                _handsService.HasItem) return;
            if (_fruitWorldService.TryTakeFromCrate(fruitId)) ClosePanel();
        }

        private void HandleFruitAmountChanged(string fruitId, int amount)
        {
            if (_entries.TryGetValue(fruitId, out FruitCrateEntryView view))
                view.SetAmount(amount, _isReady);
        }

        private void RefreshAll()
        {
            foreach (KeyValuePair<string, FruitCrateEntryView> pair in _entries)
                pair.Value.SetAmount(_inventoryService.GetFruitAmount(pair.Key), _isReady);
        }

        private void OnDisable()
        {
            if (_panel != null && _panel.activeSelf) ClosePanel();
        }

        private void OnDestroy()
        {
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(ClosePanel);
            if (_inventoryService != null) _inventoryService.FruitAmountChanged -= HandleFruitAmountChanged;
            if (_requestService != null) _requestService.FruitCrateRequested -= OpenPanel;
        }
    }
}
