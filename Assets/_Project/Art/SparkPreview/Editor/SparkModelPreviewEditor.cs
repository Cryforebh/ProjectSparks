using UnityEditor;
using UnityEngine;
namespace ProjectSparks.SparkPreview.Editor
{
    [CustomEditor(typeof(SparkModelPreview))]
    public sealed class SparkModelPreviewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var preview = (SparkModelPreview)target;
            EditorGUILayout.HelpBox("Отдельный предпросмотр. В Play Mode клип воспроизводится; вне игры параметры применяются при изменении Inspector.", MessageType.Info);
            if (DrawDefaultInspector()) { preview.Evaluate(); SceneView.RepaintAll(); }
            if (preview.Profile != null)
            {
                var clips = preview.Profile.Clips;
                for (int i = 0; i < clips.Length; i++)
                    EditorGUILayout.LabelField(i + ": " + clips[i].name);
            }
        }
    }
}
