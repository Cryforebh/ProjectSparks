using System;
using ProjectSparks.Collectibles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class FruitCrateEntryView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _amountText;

        public void Bind(FruitCatalog.Entry entry, Action<string> onSelected)
        {
            if (_button == null || _icon == null || _nameText == null || _amountText == null ||
                entry?.Prefab == null)
                throw new InvalidOperationException("Шаблон строки сундука заполнен не полностью.");

            string fruitId = entry.FruitId;
            gameObject.name = $"Fruit_{fruitId}";
            _nameText.text = entry.Prefab.DisplayName;
            _icon.sprite = entry.Icon;
            _icon.enabled = entry.Icon != null;
            _button.onClick.AddListener(() => onSelected(fruitId));
        }

        public void SetAmount(int amount, bool ready)
        {
            _amountText.text = amount.ToString();
            _button.interactable = ready && amount > 0;
        }
    }
}
