using UnityEngine;

namespace ProjectSparks.Missions
{
    [CreateAssetMenu(fileName = "RandomMissionSettings", menuName = "Project Sparks/Missions/Random Mission Settings")]
    public sealed class RandomMissionSettings : ScriptableObject
    {
        [Tooltip("Количество последовательных доставок в одной поездке.")]
        [SerializeField, Min(1)] private int _deliveryCount = 3;
        [Tooltip("Название задания. Номер доставки добавляется автоматически.")]
        [SerializeField] private string _missionTitle = "Доставка";
        [Tooltip("Количество монет за одну выполненную доставку.")]
        [SerializeField, Min(0)] private int _reward = 10;

        public int DeliveryCount => _deliveryCount;
        public string MissionTitle => _missionTitle;
        public int Reward => _reward;
    }
}
