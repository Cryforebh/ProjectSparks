using System;
using ProjectSparks.Companions;
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

        public bool TryPutDownSpark(Vector3 position, Quaternion rotation)
        {
            return PlaceSpark(position, rotation, false);
        }

        public bool TrySeatSpark(Transform seat)
        {
            return seat != null && PlaceSpark(seat.position, seat.rotation, true);
        }

        private bool PlaceSpark(Vector3 position, Quaternion rotation, bool editing)
        {
            if (HeldItem is not SparkInteractable spark)
            {
                return false;
            }

            if (editing && !spark.BeginEditing())
            {
                return false;
            }

            Transform itemTransform = spark.ItemTransform;
            itemTransform.SetParent(_originalParent, true);
            itemTransform.localScale = _originalLocalScale;
            itemTransform.SetPositionAndRotation(position, rotation);
            HeldItem = null;
            _originalParent = null;

            if (!editing)
            {
                spark.SetHeld(false);
            }

            HeldItemChanged?.Invoke(null);
            return true;
        }

        private void OnDestroy()
        {
            TryRelease();
        }
    }
}
