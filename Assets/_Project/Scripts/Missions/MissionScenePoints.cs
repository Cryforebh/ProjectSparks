using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSparks.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionScenePoints : MonoBehaviour
    {
        [Tooltip("Все точки получения груза в этой сцене.")]
        [SerializeField] private MissionScenePoint[] _pickupPoints = Array.Empty<MissionScenePoint>();
        [Tooltip("Все точки сдачи груза в этой сцене.")]
        [SerializeField] private MissionScenePoint[] _deliveryPoints = Array.Empty<MissionScenePoint>();

        public int PickupCount => _pickupPoints?.Length ?? 0;
        public int DeliveryCount => _deliveryPoints?.Length ?? 0;
        public MissionScenePoint GetPickup(int index) => _pickupPoints[index];
        public MissionScenePoint GetDelivery(int index) => _deliveryPoints[index];

        public MissionScenePoint FindPoint(string id, MissionPointKind kind)
        {
            MissionScenePoint[] points = kind == MissionPointKind.Pickup ? _pickupPoints : _deliveryPoints;
            if (points == null || string.IsNullOrWhiteSpace(id))
                return null;
            foreach (MissionScenePoint point in points)
            {
                if (point != null && point.Kind == kind &&
                    string.Equals(point.Id, id, StringComparison.Ordinal))
                    return point;
            }
            return null;
        }

        public bool ContainsPickup(MissionScenePoint point) => point != null &&
            point.Kind == MissionPointKind.Pickup && point.gameObject.scene == gameObject.scene &&
            _pickupPoints != null && Array.IndexOf(_pickupPoints, point) >= 0;

        public bool ContainsDelivery(MissionScenePoint point) => point != null &&
            point.Kind == MissionPointKind.Delivery && point.gameObject.scene == gameObject.scene &&
            _deliveryPoints != null && Array.IndexOf(_deliveryPoints, point) >= 0;

        private void Start()
        {
            if (!TryValidate(out string error))
                Debug.LogError(error, this);
        }

        [ContextMenu("Проверить точки сцены")]
        private void ValidatePoints()
        {
            if (TryValidate(out string error))
                Debug.Log($"Точки сцены проверены: складов {PickupCount}, мест доставки {DeliveryCount}.", this);
            else
                Debug.LogError(error, this);
        }

        public bool TryValidate(out string error)
        {
            var identifiers = new HashSet<string>(StringComparer.Ordinal);
            return ValidateGroup(_pickupPoints, MissionPointKind.Pickup, identifiers, out error) &&
                ValidateGroup(_deliveryPoints, MissionPointKind.Delivery, identifiers, out error);
        }

        private bool ValidateGroup(MissionScenePoint[] points, MissionPointKind kind,
            HashSet<string> identifiers, out string error)
        {
            string group = kind == MissionPointKind.Pickup ? "Склады" : "Места доставки";
            if (points == null || points.Length == 0)
            {
                error = $"{group}: список точек пуст.";
                return false;
            }

            for (int i = 0; i < points.Length; i++)
            {
                MissionScenePoint point = points[i];
                string prefix = $"{group}, точка {i + 1}";
                if (point == null)
                    error = $"{prefix}: ссылка не назначена.";
                else if (point.gameObject.scene != gameObject.scene)
                    error = $"{prefix}: точка должна принадлежать этой сцене.";
                else if (point.Kind != kind)
                    error = $"{prefix}: назначение точки не соответствует списку.";
                else if (string.IsNullOrWhiteSpace(point.Id) || !identifiers.Add(point.Id))
                    error = $"{prefix}: нужен непустой уникальный ID в пределах сцены.";
                else if (string.IsNullOrWhiteSpace(point.Label))
                    error = $"{prefix}: укажите название точки.";
                else if (kind == MissionPointKind.Pickup && point.GetComponent<MissionPickupZone>() == null)
                    error = $"{prefix}: на объекте отсутствует MissionPickupZone.";
                else if (kind == MissionPointKind.Delivery && point.GetComponent<MissionDeliveryZone>() == null)
                    error = $"{prefix}: на объекте отсутствует MissionDeliveryZone.";
                else
                    continue;

                return false;
            }

            error = null;
            return true;
        }
    }
}
