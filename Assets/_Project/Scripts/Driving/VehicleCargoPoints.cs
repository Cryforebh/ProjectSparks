using System;
using UnityEngine;

namespace ProjectSparks.Driving
{
    [DisallowMultipleComponent]
    public sealed class VehicleCargoPoints : MonoBehaviour
    {
        [Tooltip("Точки размещения посылок на этой машине.")]
        [SerializeField] private Transform[] _points = Array.Empty<Transform>();

        public int Count => _points.Length;

        public Transform GetPoint(int index)
        {
            return _points[index];
        }
    }
}
