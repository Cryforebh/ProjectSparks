using System;
using ProjectSparks.Collectibles;
using ProjectSparks.Companions;
using UnityEngine;

namespace ProjectSparks.Infrastructure
{
    [Serializable]
    public sealed class GameProgressData
    {
        [Tooltip("Сохраненное количество монет.")]
        [SerializeField] private int _balance;
        [Tooltip("Сохраненный тип выбранного компаньона.")]
        [SerializeField] private CompanionType _selectedCompanion;
        [Tooltip("Сохраненное количество фруктов.")]
        [SerializeField] private int _fruitCount;
        [Tooltip("Сохраненное количество капсул.")]
        [SerializeField] private int _capsuleCount;
        [Tooltip("Сохраненные данные Искры-Собирателя.")]
        [SerializeField] private SparkProgressData _collectorSpark;
        [Tooltip("Сохраненные данные Искры-Механика.")]
        [SerializeField] private SparkProgressData _mechanicSpark;
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

        public GameProgressData(
            int balance,
            CompanionType selectedCompanion,
            int fruitCount,
            int capsuleCount,
            SparkProgressData collectorSpark = null,
            SparkProgressData mechanicSpark = null,
            int swimFruitCount = 0,
            int flyFruitCount = 0,
            int runFruitCount = 0,
            int powerFruitCount = 0,
            int staminaFruitCount = 0)
        {
            _balance = Math.Max(0, balance);
            _selectedCompanion = selectedCompanion;
            _fruitCount = Math.Max(0, fruitCount);
            _capsuleCount = Math.Max(0, capsuleCount);
            _collectorSpark = collectorSpark;
            _mechanicSpark = mechanicSpark;
            _swimFruitCount = Math.Max(0, swimFruitCount);
            _flyFruitCount = Math.Max(0, flyFruitCount);
            _runFruitCount = Math.Max(0, runFruitCount);
            _powerFruitCount = Math.Max(0, powerFruitCount);
            _staminaFruitCount = Math.Max(0, staminaFruitCount);
        }

        public int Balance => _balance;
        public CompanionType SelectedCompanion => _selectedCompanion;
        public int FruitCount => _fruitCount;
        public int CapsuleCount => _capsuleCount;
        public SparkProgressData CollectorSpark => _collectorSpark;
        public SparkProgressData MechanicSpark => _mechanicSpark;
        public int SwimFruitCount => _swimFruitCount;
        public int FlyFruitCount => _flyFruitCount;
        public int RunFruitCount => _runFruitCount;
        public int PowerFruitCount => _powerFruitCount;
        public int StaminaFruitCount => _staminaFruitCount;
    }
}
