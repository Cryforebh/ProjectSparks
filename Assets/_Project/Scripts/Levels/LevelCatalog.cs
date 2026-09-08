using System;
using UnityEngine;

namespace ProjectSparks.Levels
{
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Project Sparks/Levels/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Описание локации в списке выбора.")]
            [SerializeField] private LevelDefinition _level;
            [Tooltip("Разрешить поездки в эту локацию.")]
            [SerializeField] private bool _isAvailable;

            public LevelDefinition Level => _level;
            public bool IsAvailable => _isAvailable;
        }

        [Tooltip("Локации в порядке отображения в меню.")]
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public int Count => _entries?.Length ?? 0;
        public Entry GetEntry(int index) => _entries[index];

        public bool CanLaunch(LevelDefinition level)
        {
            if (level == null || string.IsNullOrWhiteSpace(level.Id) ||
                string.IsNullOrWhiteSpace(level.SceneName))
                return false;

            int matches = 0;
            bool available = false;
            for (int i = 0; i < Count; i++)
            {
                Entry entry = _entries[i];
                if (entry?.Level == null)
                    continue;
                if (entry.Level.Id == level.Id || entry.Level.SceneName == level.SceneName)
                    matches++;
                if (entry.Level == level)
                    available = entry.IsAvailable;
            }

            return available && matches == 1;
        }

        public string GetLocationId(string sceneName)
        {
            for (int i = 0; i < Count; i++)
            {
                LevelDefinition level = _entries[i]?.Level;
                if (level != null && level.SceneName == sceneName && !string.IsNullOrWhiteSpace(level.Id))
                    return level.Id;
            }

            return sceneName;
        }
    }
}
