using System.Collections.Generic;
using ProjectSparks.Collectibles;
using UnityEditor;
using UnityEngine;

namespace ProjectSparks.Editor
{
    [CustomEditor(typeof(CollectibleResource)), CanEditMultipleObjects]
    public sealed class CollectibleResourceEditor : UnityEditor.Editor
    {
        private SerializedProperty _resourceType;
        private SerializedProperty _amount;
        private SerializedProperty _fruitPrefab;
        private FruitCatalog _catalog;
        private bool _chooseCatalog;

        private void OnEnable()
        {
            _resourceType = serializedObject.FindProperty("_resourceType");
            _amount = serializedObject.FindProperty("_amount");
            _fruitPrefab = serializedObject.FindProperty("_fruitPrefab");

            string[] catalogs = AssetDatabase.FindAssets("t:FruitCatalog");
            _chooseCatalog = catalogs.Length != 1;
            if (catalogs.Length == 1)
                _catalog = AssetDatabase.LoadAssetAtPath<FruitCatalog>(AssetDatabase.GUIDToAssetPath(catalogs[0]));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            EditorGUILayout.PropertyField(_resourceType, new GUIContent("Ресурс"));
            EditorGUILayout.PropertyField(_amount, new GUIContent("Количество"));

            if (_resourceType.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Для выбора фрукта выделите объекты с одинаковым видом ресурса.", MessageType.Info);
            }
            else if ((ResourceType)_resourceType.intValue == ResourceType.Fruit)
            {
                DrawFruitSelection();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFruitSelection()
        {
            if (_chooseCatalog || _catalog == null)
                _catalog = (FruitCatalog)EditorGUILayout.ObjectField("Каталог фруктов", _catalog, typeof(FruitCatalog), false);

            if (_catalog == null)
            {
                EditorGUILayout.HelpBox("Выберите каталог, из которого нужно взять фрукт для награды.", MessageType.Warning);
                return;
            }

            var labels = new List<GUIContent> { new GUIContent("Не выбран") };
            var prefabs = new List<PhysicalFruitItem> { null };
            PhysicalFruitItem current = _fruitPrefab.objectReferenceValue as PhysicalFruitItem;
            int selected = 0;
            for (int i = 0; i < _catalog.Count; i++)
            {
                PhysicalFruitItem prefab = _catalog.GetEntry(i)?.Prefab;
                if (prefab == null || string.IsNullOrWhiteSpace(prefab.FruitId))
                    continue;

                prefabs.Add(prefab);
                labels.Add(new GUIContent($"{prefab.DisplayName} ({prefab.FruitId})"));
                if (prefab == current)
                    selected = prefabs.Count - 1;
            }

            if (current != null && selected == 0 && !_fruitPrefab.hasMultipleDifferentValues)
            {
                selected = prefabs.Count;
                prefabs.Add(current);
                labels.Add(new GUIContent($"{current.DisplayName} — отсутствует в каталоге"));
                EditorGUILayout.HelpBox("Назначенный фрукт отсутствует в выбранном каталоге. Выберите актуальный вариант.", MessageType.Warning);
            }

            Rect rect = EditorGUILayout.GetControlRect();
            var label = new GUIContent("Фрукт", _fruitPrefab.tooltip);
            EditorGUI.BeginProperty(rect, label, _fruitPrefab);
            EditorGUI.showMixedValue = _fruitPrefab.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int choice = EditorGUI.Popup(rect, label, selected, labels.ToArray());
            if (EditorGUI.EndChangeCheck())
                _fruitPrefab.objectReferenceValue = prefabs[choice];
            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();

            if (!_fruitPrefab.hasMultipleDifferentValues && _fruitPrefab.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Фрукт не выбран: этот объект не сможет начислить фрукт при подборе.", MessageType.Warning);
        }
    }
}
