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
        [Tooltip("Количество монет за выполнение миссии.")]
        [SerializeField, Min(0)] private int _reward;

        public string Id => _id;
        public string Title => _title;
        public int Reward => _reward;
    }
}
