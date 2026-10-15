using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectSparks.Editor
{
    public sealed class RoadBuilderWindow : EditorWindow
    {
        private const string ModelsFolder = "Assets/_Project/Models/Driving/Roads";
        private const string PrefabsFolder = "Assets/_Project/Prefabs/Driving/Roads";

        private GameObject[] _roadPrefabs = Array.Empty<GameObject>();
        private Transform _targetRoad;
        private int _targetPointIndex;
        private int _roadIndex;
        private int _newPointIndex;

        [MenuItem("Tools/Roads/Road Builder")]
        private static void Open()
        {
            GetWindow<RoadBuilderWindow>("Road Builder");
        }

        private void OnEnable()
        {
            RefreshRoadPrefabs();
            SceneView.duringSceneGui += DrawConnectionHandles;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawConnectionHandles;
        }

        private void OnSelectionChange()
        {
            if (Selection.activeTransform != null && FindRoadRoot(Selection.activeTransform) != null)
            {
                SetTargetRoad(Selection.activeTransform);
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Подготовка секций", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Создайте префабы один раз. Коллайдер получит только дорожное полотно; FBX и существующие префабы не изменяются.", MessageType.Info);

            if (GUILayout.Button("Создать недостающие префабы"))
                CreateMissingPrefabs();

            if (_roadPrefabs.Length == 0)
            {
                EditorGUILayout.HelpBox("Готовых префабов пока нет.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Присоединить секцию", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Выберите дорожную секцию или ее ConnectionPoint в Hierarchy. Затем укажите секцию и конец, которым она должна присоединиться.", MessageType.None);

            var selectedTarget = (Transform)EditorGUILayout.ObjectField("Секция в сцене", _targetRoad, typeof(Transform), true);
            if (selectedTarget != _targetRoad)
                SetTargetRoad(selectedTarget);

            var targetPoints = GetConnectionPoints(_targetRoad);
            if (targetPoints.Count > 0)
            {
                _targetPointIndex = Mathf.Clamp(_targetPointIndex, 0, targetPoints.Count - 1);
                _targetPointIndex = EditorGUILayout.Popup("Ее точка", _targetPointIndex, GetPointNames(targetPoints));
            }
            else
            {
                EditorGUILayout.HelpBox("Выберите готовую дорожную секцию в сцене.", MessageType.Warning);
            }

            _roadIndex = Mathf.Clamp(_roadIndex, 0, _roadPrefabs.Length - 1);
            _roadIndex = EditorGUILayout.Popup("Новая секция", _roadIndex, GetRoadNames());
            var newPoints = GetConnectionPoints(_roadPrefabs[_roadIndex].transform);
            _newPointIndex = Mathf.Clamp(_newPointIndex, 0, Mathf.Max(0, newPoints.Count - 1));
            if (newPoints.Count > 0)
                _newPointIndex = EditorGUILayout.Popup("Ее точка", _newPointIndex, GetPointNames(newPoints));

            using (new EditorGUI.DisabledScope(targetPoints.Count == 0 || newPoints.Count == 0 || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Присоединить", GUILayout.Height(32)))
                    AttachRoad(targetPoints[_targetPointIndex]);
            }
        }

        private void SetTargetRoad(Transform selected)
        {
            var root = FindRoadRoot(selected);
            if (root != _targetRoad)
                _targetPointIndex = 0;

            _targetRoad = root;
            if (root == null || selected == root)
                return;

            var points = GetConnectionPoints(root);
            int selectedPoint = points.IndexOf(selected);
            if (selectedPoint >= 0)
                _targetPointIndex = selectedPoint;
        }

        private static Transform FindRoadRoot(Transform selected)
        {
            if (selected == null || !selected.gameObject.scene.IsValid())
                return null;

            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(selected.gameObject);
            if (root == null)
                return null;

            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            return path.StartsWith(PrefabsFolder + "/", StringComparison.Ordinal) ? root.transform : null;
        }

        private void RefreshRoadPrefabs()
        {
            var prefabs = new List<GameObject>();
            if (AssetDatabase.IsValidFolder(PrefabsFolder))
            {
                foreach (string guid in AssetDatabase.FindAssets("", new[] { PrefabsFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                        prefabs.Add(prefab);
                }
            }

            prefabs.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            _roadPrefabs = prefabs.ToArray();
            _roadIndex = Mathf.Clamp(_roadIndex, 0, Mathf.Max(0, _roadPrefabs.Length - 1));
        }

        private void CreateMissingPrefabs()
        {
            try
            {
                var models = new List<GameObject>();
                foreach (string guid in AssetDatabase.FindAssets("", new[] { ModelsFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (model == null || FindSurface(model) == null || GetConnectionPoints(model.transform).Count == 0)
                        throw new InvalidOperationException("Не найдены полотно или точки подключения: " + path);

                    models.Add(model);
                }

                if (models.Count == 0)
                    throw new InvalidOperationException("В папке моделей дорог нет FBX.");

                if (!AssetDatabase.IsValidFolder(PrefabsFolder))
                    AssetDatabase.CreateFolder("Assets/_Project/Prefabs/Driving", "Roads");

                int created = 0;
                foreach (var model in models)
                {
                    string path = PrefabsFolder + "/" + model.name + ".prefab";
                    if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                        continue;

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    try
                    {
                        var surface = FindSurface(instance);
                        var collider = surface.GetComponent<MeshCollider>();
                        if (collider == null)
                            collider = surface.gameObject.AddComponent<MeshCollider>();

                        collider.sharedMesh = surface.sharedMesh;
                        collider.convex = false;

                        PrefabUtility.SaveAsPrefabAsset(instance, path, out bool saved);
                        if (!saved)
                            throw new InvalidOperationException("Не удалось сохранить префаб: " + path);

                        created++;
                    }
                    finally
                    {
                        DestroyImmediate(instance);
                    }
                }

                RefreshRoadPrefabs();
                EditorUtility.DisplayDialog("Road Builder", "Создано префабов: " + created + ". Уже существующие не менялись.", "OK");
            }
            catch (Exception error)
            {
                EditorUtility.DisplayDialog("Road Builder", error.Message, "OK");
            }
        }

        private void AttachRoad(Transform targetPoint)
        {
            if (!IsUnitScale(_targetRoad.lossyScale))
            {
                EditorUtility.DisplayDialog("Road Builder", "Масштаб выбранной секции и ее родителей должен быть (1, 1, 1).", "OK");
                return;
            }

            var prefab = _roadPrefabs[_roadIndex];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _targetRoad.gameObject.scene);
            if (instance == null)
                return;

            try
            {
                var points = GetConnectionPoints(instance.transform);
                var newPoint = points[_newPointIndex];
                if (!IsUnitScale(instance.transform.lossyScale))
                    throw new InvalidOperationException("Масштаб нового префаба должен быть (1, 1, 1).");

                Quaternion pointRotationFromRoot = Quaternion.Inverse(instance.transform.rotation) * newPoint.rotation;
                Quaternion facingTarget = targetPoint.rotation * Quaternion.Euler(0f, 180f, 0f);
                instance.transform.rotation = facingTarget * Quaternion.Inverse(pointRotationFromRoot);
                instance.transform.position += targetPoint.position - newPoint.position;

                Undo.RegisterCreatedObjectUndo(instance, "Присоединить дорогу");
                EditorSceneManager.MarkSceneDirty(instance.scene);
                Selection.activeGameObject = instance;
                SetTargetRoad(instance.transform);
                _targetPointIndex = points.Count > 1 && _newPointIndex == 0 ? 1 : 0;
                SceneView.RepaintAll();
            }
            catch (Exception error)
            {
                DestroyImmediate(instance);
                EditorUtility.DisplayDialog("Road Builder", error.Message, "OK");
            }
        }

        private void DrawConnectionHandles(SceneView sceneView)
        {
            var points = GetConnectionPoints(_targetRoad);
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                float size = HandleUtility.GetHandleSize(point.position) * 0.12f;
                Handles.color = i == _targetPointIndex ? Color.yellow : Color.cyan;
                if (Handles.Button(point.position, point.rotation, size, size, Handles.SphereHandleCap))
                {
                    _targetPointIndex = i;
                    Repaint();
                }

                Handles.Label(point.position + Vector3.up * size, point.name.Substring(point.name.LastIndexOf('_') + 1));
            }
        }

        private static MeshFilter FindSurface(GameObject model)
        {
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
                if (filter.name.EndsWith("_Surface", StringComparison.Ordinal) && filter.sharedMesh != null)
                    return filter;

            return null;
        }

        private static List<Transform> GetConnectionPoints(Transform root)
        {
            var points = new List<Transform>();
            if (root == null)
                return points;

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child != root && child.name.Contains("_ConnectionPoint_"))
                    points.Add(child);

            points.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return points;
        }

        private string[] GetRoadNames()
        {
            var names = new string[_roadPrefabs.Length];
            for (int i = 0; i < names.Length; i++)
                names[i] = _roadPrefabs[i].name;

            return names;
        }

        private static string[] GetPointNames(List<Transform> points)
        {
            var names = new string[points.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = points[i].name.Substring(points[i].name.LastIndexOf('_') + 1);

            return names;
        }

        private static bool IsUnitScale(Vector3 scale)
        {
            return Mathf.Abs(scale.x - 1f) < 0.001f
                   && Mathf.Abs(scale.y - 1f) < 0.001f
                   && Mathf.Abs(scale.z - 1f) < 0.001f;
        }
    }
}
