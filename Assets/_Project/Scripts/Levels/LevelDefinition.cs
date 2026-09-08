using UnityEngine;

namespace ProjectSparks.Levels
{
    [CreateAssetMenu(
        fileName = "Level_",
        menuName = "Project Sparks/Levels/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Tooltip("Постоянный уникальный идентификатор локации.")]
        [SerializeField] private string _id;
        [Tooltip("Название локации в окне выбора уровня.")]
        [SerializeField] private string _title;
        [Tooltip("Краткое описание локации для игрока.")]
        [SerializeField, TextArea(2, 5)] private string _description;
        [Tooltip("Имя сцены локации из списка сцен сборки.")]
        [SerializeField] private string _sceneName;

        public string Id => _id;
        public string Title => _title;
        public string Description => _description;
        public string SceneName => _sceneName;
    }
}
