using UnityEngine;

namespace ProjectSparks.Missions
{
    [CreateAssetMenu(
        fileName = "Mission_",
        menuName = "Project Sparks/Missions/Mission Definition")]
    public sealed class MissionDefinition : ScriptableObject
    {
        [Tooltip("Постоянный уникальный идентификатор миссии.")]
        [SerializeField] private string _id;
        [Tooltip("Название миссии для интерфейса.")]
        [SerializeField] private string _title;
        [Tooltip("Название точки получения груза.")]
        [SerializeField] private string _pickupLabel;
        [Tooltip("Название точки сдачи груза.")]
        [SerializeField] private string _destinationLabel;
        [Tooltip("Количество монет за выполнение миссии.")]
        [SerializeField, Min(0)] private int _reward;

        public string Id => _id;
        public string Title => _title;
        public string PickupLabel => _pickupLabel;
        public string DestinationLabel => _destinationLabel;
        public int Reward => _reward;
    }
}
