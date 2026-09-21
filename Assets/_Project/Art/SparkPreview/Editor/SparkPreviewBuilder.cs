using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ProjectSparks.SparkPreview.Editor
{
    [InitializeOnLoad] public static class SparkPreviewBuilder
    {
        private const string Folder = "Assets/_Project/Art/SparkPreview";
        static SparkPreviewBuilder() { EditorApplication.update += ProcessRequest; }
        private static void ProcessRequest()
        {
            const string request = "H:/Diplom/Verification/SparkPreview/Build.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { SparkImportAudit.Run(); Build(); File.WriteAllText("H:/Diplom/Verification/SparkPreview/Build.result", "OK"); }
            catch (Exception ex) { File.WriteAllText("H:/Diplom/Verification/SparkPreview/Build.result", ex.ToString()); Debug.LogException(ex); }
        }
        [Serializable] public class Contract { public ClipInfo[] clips; public Calibration[] calibrations; public MaterialInfo[] materials; public Check[] checks; }
        [Serializable] public class ClipInfo { public string name, take; public float duration; public bool loop; public GroundSample[] ground; public string[] groundFallback; }
        [Serializable] public class Calibration { public string file, kind; public Control[] controls; public Sample[] samples; }
        [Serializable] public class Control { public string path; public bool position, rotation, scale; }
        [Serializable] public class Sample { public float time; }
        [Serializable] public class MaterialInfo { public string name, baseColorTexture; public float roughness, metallic; public bool doubleSided; }
        [Serializable] public class Check { public string clip; public float time, growth, earDevelopment, earRest; public Joint[] jointsBlender; public Box boundsBlender; }
        [Serializable] public class Joint { public string name; public float[] position; }
        [Serializable] public class Box { public float[] min, max; }

        [MenuItem("Tools/Project Sparks/Build Spark Preview")]
        public static void Build()
        {
            var contract = JsonUtility.FromJson<Contract>(File.ReadAllText(Folder + "/Source/Spark_Import.json"));
            Directory.CreateDirectory(Folder + "/Materials");
            AssetDatabase.Refresh();
            var materialMap = new Dictionary<string, Material>();
            foreach (var info in contract.materials)
            {
                string path = Folder + "/Materials/" + info.name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Source/Textures/" + info.baseColorTexture);
                if (texture == null) throw new InvalidOperationException("Missing texture: " + info.baseColorTexture);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetTexture("_BaseMap", texture);
                mat.SetFloat("_Smoothness", 1 - info.roughness);
                mat.SetFloat("_Metallic", info.metallic);
                mat.SetFloat("_Cull", info.doubleSided ? 0 : 2);
                EditorUtility.SetDirty(mat);
                materialMap.Add(info.name, mat);
            }
            string modelPath = Folder + "/Source/Spark_PreBlink.fbx";
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var bones = new List<BoneCalibration>();
            foreach (var calibration in contract.calibrations)
            {
                string path = Folder + "/Source/" + calibration.file;
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var temporaryScene = EditorSceneManager.NewPreviewScene();
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, temporaryScene);
                try
                {
                    var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__"));
                    var tracks = calibration.controls.Select(c => new BoneCalibration { path = c.path, kind = calibration.kind, position = c.position, rotation = c.rotation, scale = c.scale, samples = new LocalPose[calibration.samples.Length] }).ToArray();
                    foreach (var track in tracks)
                        if (instance.transform.Find(track.path) == null || modelAsset.transform.Find(track.path) == null) throw new InvalidOperationException("Calibration hierarchy mismatch: " + track.path);
                    for (int i = 0; i < calibration.samples.Length; i++)
                    {
                        clip.SampleAnimation(instance, calibration.samples[i].time);
                        foreach (var track in tracks) track.samples[i] = LocalPose.Read(instance.transform.Find(track.path));
                    }
                    bones.AddRange(tracks);
                }
                finally { EditorSceneManager.ClosePreviewScene(temporaryScene); }
            }
            var sourceClips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            var clips = contract.clips.Select(info =>
            {
                var clip = sourceClips.Single(c => c.name == info.take);
                if (Mathf.Abs(clip.length - info.duration) > 0.001f) throw new InvalidOperationException("Clip duration mismatch: " + info.name);
                var ground = info.ground;
                
                return new PreviewClip { name = info.name, clip = clip, loop = info.loop, ground = ground };
            }).ToArray();
            string profilePath = Folder + "/SparkPreviewProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<SparkPreviewProfile>(profilePath);
            if (profile == null) { profile = ScriptableObject.CreateInstance<SparkPreviewProfile>(); AssetDatabase.CreateAsset(profile, profilePath); }
            profile.Initialize(clips, bones.ToArray()); EditorUtility.SetDirty(profile);
            var workingScene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("SparkPreview");
            SceneManager.MoveGameObjectToScene(root, workingScene);
            try
            {
                var model = UnityEngine.Object.Instantiate(modelAsset, root.transform, false);
                model.name = "Model";
                foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materialMap[m.name]).ToArray();
                    renderer.updateWhenOffscreen = true;
                }
                var animator = model.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;
                var preview = root.AddComponent<SparkModelPreview>();
                preview.Initialize(profile, model.transform);
                preview.Evaluate();
                Validate(preview, contract);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/SparkPreview.prefab");
                CreateScene(prefab);
            }
            finally { EditorSceneManager.ClosePreviewScene(workingScene); }
            AssetDatabase.SaveAssets();
        }

        private static void Validate(SparkModelPreview preview, Contract contract)
        {
            var report = new StringBuilder();
            foreach (var check in contract.checks)
            {
                int index = Array.FindIndex(preview.Profile.Clips, c => c.name == check.clip);
                var clip = preview.Profile.Clips[index].clip;
                preview.SetPreview(index, check.time / clip.length, check.growth, check.earDevelopment, check.earRest);
                float normal = 0, flipped = 0;
                var transforms = preview.Model.GetComponentsInChildren<Transform>().ToDictionary(t => t.name);
                foreach (var joint in check.jointsBlender)
                {
                    var actual = transforms[joint.name].position;
                    var p = joint.position;
                    normal = Mathf.Max(normal, Vector3.Distance(actual, new Vector3(p[0], p[2], -p[1])));
                    flipped = Mathf.Max(flipped, Vector3.Distance(actual, new Vector3(-p[0], p[2], -p[1])));
                }
                report.AppendLine($"{check.clip} g={check.growth} time={check.time} jointErrorXYZ={normal:F7} mirroredX={flipped:F7}");
                foreach (var renderer in preview.Model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh = new Mesh(); renderer.BakeMesh(mesh);
                    var points = mesh.vertices.Select(renderer.transform.TransformPoint).ToArray();
                    var bounds = new Bounds(points[0], Vector3.zero);
                    foreach (var p in points) bounds.Encapsulate(p);
                    report.AppendLine($" {renderer.name} min={bounds.min:F6} max={bounds.max:F6}");
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }
            Directory.CreateDirectory("H:/Diplom/Verification/SparkPreview");
            File.WriteAllText("H:/Diplom/Verification/SparkPreview/PoseChecks.txt", report.ToString());
            preview.SetPreview(Array.FindIndex(preview.Profile.Clips, c => c.name == "Ilde_Sit_Static"), 0, 1, 1, 1);
        }
        private static void CreateScene(GameObject prefab)
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                PrefabUtility.InstantiatePrefab(prefab, scene);
                var cameraObject = new GameObject("PreviewCamera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = new Vector3(1.25f, 0.9f, 1.7f); camera.transform.LookAt(new Vector3(0, 0.3f, 0));
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.12f, 0.14f, 0.17f);
                camera.nearClipPlane = 0.01f;
                var lightObject = new GameObject("PreviewLight"); SceneManager.MoveGameObjectToScene(lightObject, scene);
                var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2;
                light.transform.rotation = Quaternion.Euler(45, -30, 0);
                EditorSceneManager.SaveScene(scene, Folder + "/SparkPreview.unity");
            }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
        }
    }
}
