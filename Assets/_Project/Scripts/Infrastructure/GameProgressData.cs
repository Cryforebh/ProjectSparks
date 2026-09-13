using System;
using ProjectSparks.Companions;
using UnityEngine;

namespace ProjectSparks.Infrastructure
{
    [Serializable]
    public sealed class GameProgressData
    {
        [Tooltip("Сохраненное количество монет.")]
        [SerializeField] private int _balance;
        [Tooltip("Сохраненное количество капсул.")]
        [SerializeField] private int _capsuleCount;
        [Tooltip("Сохраненное количество фруктов плавания.")]
        [SerializeField] private int _swimFruitCount;
        [Tooltip("Сохраненное количество фруктов полета.")]
        [SerializeField] private int _flyFruitCount;
        [Tooltip("Сохраненное количество фруктов бега.")]
        [SerializeField] private int _runFruitCount;
        [Tooltip("Сохраненное количество фруктов силы.")]
        [SerializeField] private int _powerFruitCount;
        [Tooltip("Сохраненное количество фруктов выносливости.")]
        [SerializeField] private int _staminaFruitCount;
        [Tooltip("Идентификатор последней сохраненной поездки для защиты от повторной награды.")]
        [SerializeField] private string _lastCommittedTripId;

        [Tooltip("ID выбранной конкретной Искры.")]
        [SerializeField] private string _selectedSparkId;
        [Tooltip("Все сохраненные Искры с постоянными идентификаторами.")]
        [SerializeField] private SparkProgressData[] _sparks;

        public GameProgressData(
            int balance,
            string selectedCompanion,
            int capsuleCount,
            int swimFruitCount = 0,
            int flyFruitCount = 0,
            int runFruitCount = 0,
            int powerFruitCount = 0,
            int staminaFruitCount = 0,
            string lastCommittedTripId = null,
            SparkProgressData[] sparks = null)
        {
            _balance = Math.Max(0, balance);
            _selectedSparkId = selectedCompanion ?? string.Empty;
            _sparks = sparks;
            _capsuleCount = Math.Max(0, capsuleCount);
            _swimFruitCount = Math.Max(0, swimFruitCount);
            _flyFruitCount = Math.Max(0, flyFruitCount);
            _runFruitCount = Math.Max(0, runFruitCount);
            _powerFruitCount = Math.Max(0, powerFruitCount);
            _staminaFruitCount = Math.Max(0, staminaFruitCount);
            _lastCommittedTripId = lastCommittedTripId;
        }

        public int Balance => _balance;
        public string SelectedCompanion => _selectedSparkId ?? string.Empty;
        public SparkProgressData[] Sparks => _sparks;
        public int CapsuleCount => _capsuleCount;
        public int SwimFruitCount => _swimFruitCount;
        public int FlyFruitCount => _flyFruitCount;
        public int RunFruitCount => _runFruitCount;
        public int PowerFruitCount => _powerFruitCount;
        public int StaminaFruitCount => _staminaFruitCount;
        public string LastCommittedTripId => _lastCommittedTripId;
    }
}
