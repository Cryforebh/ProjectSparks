using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectSparks.SparkPreview.Editor
{
    [InitializeOnLoad]
    public static class SparkFacingCheck
    {
        private const string Report = "H:/Diplom/Verification/SparkPreview/FacingCheck.txt";
        static SparkFacingCheck() { EditorApplication.update += Check; }
        private static void Check()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= Check;
            if (File.Exists(Report)) return;
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                const string path = "Assets/_Project/Art/SparkPreview/Source/Spark_PreBlink.fbx";
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
                clips.Single(c => c.name.EndsWith("|Ilde_Sit_Static")).SampleAnimation(model, 0);
                var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
                var eyes = renderers.Single(r => r.name == "SparkEyes");
                var mesh = new Mesh();
                eyes.BakeMesh(mesh);
                Vector3 center = Vector3.zero;
                foreach (var p in mesh.vertices) center += eyes.transform.TransformPoint(p);
                center /= mesh.vertexCount;
                UnityEngine.Object.DestroyImmediate(mesh);
                var head = model.GetComponentsInChildren<Transform>().Single(t => t.name == "Head");
                float facing = (center - head.position).z;
                int shapes = renderers.Sum(r => r.sharedMesh.blendShapeCount);
                bool passed = facing > 0 && clips.Length == 14 && renderers.Length == 4 && shapes == 3;
                File.WriteAllText(Report, $"Passed={passed}\nFace offset Z={facing:R}\nClips={clips.Length}\nMeshes={renderers.Length}\nShapes={shapes}\nRoot rotation={model.transform.localRotation}\n");
            }
            catch (Exception ex) { File.WriteAllText(Report, ex.ToString()); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
