using System;
using UnityEngine;

namespace ProjectSparks.Infrastructure
{
    [Serializable]
    public sealed class GameProgressData
    {
        [Tooltip("Сохраненное количество монет.")]
        [SerializeField] private int _balance;

        public GameProgressData(int balance)
        {
            _balance = Math.Max(0, balance);
        }

        public int Balance => _balance;
    }
}
