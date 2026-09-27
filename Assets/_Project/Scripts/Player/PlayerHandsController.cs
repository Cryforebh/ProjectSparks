using System;
using UnityEngine;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerHandsController : MonoBehaviour, IPlayerHandsService
    {
        [Tooltip("Точка перед камерой, в которой отображается предмет в руках.")]
        [SerializeField] private Transform _holdAnchor;

        private Transform _originalParent;
        private Vector3 _originalLocalPosition;
        private Quaternion _originalLocalRotation;
        private Vector3 _originalLocalScale;

        public event Action<IHoldableItem> HeldItemChanged;

        public IHoldableItem HeldItem { get; private set; }
        public bool HasItem => HeldItem != null;

        private void Awake()
        {
            if (_holdAnchor == null)
            {
                Debug.LogError("В PlayerHandsController не назначена точка предмета в руках.", this);
                enabled = false;
            }
        }

        public bool TryHold(IHoldableItem item)
        {
            if (!enabled || item == null || HasItem || item.ItemTransform == null)
            {
                return false;
            }

            Transform itemTransform = item.ItemTransform;
            _originalParent = itemTransform.parent;
            _originalLocalPosition = itemTransform.localPosition;
            _originalLocalRotation = itemTransform.localRotation;
            _originalLocalScale = itemTransform.localScale;

            HeldItem = item;
            item.SetHeld(true);
            itemTransform.SetParent(_holdAnchor, false);
            itemTransform.localPosition = Vector3.zero;
            itemTransform.localRotation = item.ItemType == HeldItemType.Spark
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;
            HeldItemChanged?.Invoke(HeldItem);
            return true;
        }

        public bool TryRelease()
        {
            if (!HasItem)
            {
                return false;
            }

            IHoldableItem releasedItem = HeldItem;
            Transform itemTransform = releasedItem.ItemTransform;
            HeldItem = null;

            if (itemTransform != null)
            {
                itemTransform.SetParent(_originalParent, false);
                itemTransform.localPosition = _originalLocalPosition;
                itemTransform.localRotation = _originalLocalRotation;
                itemTransform.localScale = _originalLocalScale;
            }

            releasedItem.SetHeld(false);
            _originalParent = null;
            HeldItemChanged?.Invoke(null);
            return true;
        }

        private void OnDestroy()
        {
            TryRelease();
        }
    }
}
