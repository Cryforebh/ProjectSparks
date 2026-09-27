using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectSparks.Editor
{
    public static class Spark01Import
    {
        private const string Root = "Assets/_Project/Art/SparkPreview";
        private const string Source = Root + "/Source";

        [Serializable] private class Manifest { public ClipInfo[] clips; public MeshInfo[] meshes; public MatInfo[] materials; }
        [Serializable] private class ClipInfo { public string name; public string file; public float startFrame; public float endFrame; public float duration; public bool loop; }
        [Serializable] private class MeshInfo { public string name; public string[] materials; public int triangles; }
        [Serializable] private class MatInfo { public string name; public string texture; public float[] color; public float roughness; public float metallic; }

        [MenuItem("Tools/Spark/Configure and Validate Spark_01")]
        public static void Run()
        {
            var report = new StringBuilder();
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Source + "/Spark_01_Import.json"));
            var materialPaths = new Dictionary<string, string>
            {
                ["SparkBody_Material"] = Root + "/Materials/SparkBody_Masked_TestRGB 2.mat",
                ["Claws_Material"] = Root + "/Materials/Claws_Material.mat",
                ["SparkEars_Material"] = Root + "/Materials/SparkEars_Masked_TestRGB 2.mat",
                ["Spark_Eyes_Material"] = Root + "/Materials/Spark_Eyes.mat",
                ["SparkHead_Material"] = Root + "/Materials/SparkHead_Masked_TestRGB 2.mat",
                ["Antennas_Material"] = Root + "/Materials/Scales_Material.mat"
            };
            var map = new Dictionary<string, Material>();
            foreach (var entry in materialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(entry.Value);
                if (material == null) throw new Exception("Missing material: " + entry.Value);
                map[entry.Key] = material;
            }
            foreach (var material in manifest.materials)
                if (!map.ContainsKey(material.name)) throw new Exception("Unmapped material: " + material.name);
            foreach (var item in map) report.AppendLine("Material " + item.Key + " -> " + AssetDatabase.GetAssetPath(item.Value));

            var importedClips = new List<AnimationClip>();
            foreach (var info in manifest.clips)
            {
                string path = Source + "/" + info.file;
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) throw new Exception("Missing model importer: " + path);
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.bakeAxisConversion = true;
                importer.globalScale = 1;
                importer.useFileScale = true;
                importer.preserveHierarchy = false;
                importer.optimizeGameObjects = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importBlendShapes = true;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importBlendShapeNormals = ModelImporterNormals.Import;
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                importer.resampleCurves = true;
                importer.importAnimation = true;
                importer.isReadable = true;
                foreach (var entry in map) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.Key), entry.Value);
                var takes = importer.defaultClipAnimations;
                if (takes.Length != 1) throw new Exception(path + " has " + takes.Length + " takes");
                var take = takes[0];
                take.name = info.name;
                take.firstFrame = info.startFrame;
                take.lastFrame = info.endFrame;
                take.loopTime = info.loop;
                take.loopPose = false;
                take.keepOriginalOrientation = true;
                take.keepOriginalPositionY = true;
                take.keepOriginalPositionXZ = true;
                var existingClip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                var events = existingClip != null ? AnimationUtility.GetAnimationEvents(existingClip) : Array.Empty<AnimationEvent>();
                if (info.name == "Ilde_Sit_Eat" && !events.Any(e => e.functionName == "CompleteEating"))
                    throw new Exception("Missing CompleteEating event on existing eating clip");
                foreach (var ev in events)
                    ev.time = existingClip.length > 0 ? Mathf.Clamp01(ev.time / existingClip.length) : 0;
                take.events = events;
                importer.clipAnimations = new[] { take };
                importer.SaveAndReimport();
                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__"));
                importedClips.Add(clip);
                if (Mathf.Abs(clip.length - info.duration) > .002f) throw new Exception("Duration mismatch " + info.name + " actual=" + clip.length + " expected=" + info.duration + " rate=" + clip.frameRate + " takeEnd=" + takes[0].lastFrame);
                report.AppendLine("Clip " + clip.name + " duration=" + clip.length + " events=" + AnimationUtility.GetAnimationEvents(clip).Length);
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "/Spark_01.fbx");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = "Spark_01";
                var animator = instance.GetComponent<Animator>();
                if (animator != null) animator.applyRootMotion = false;
                importedClips.Single(c => c.name == "Ilde_Sit_Static").SampleAnimation(instance, 0);
                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (renderers.Length != 4) throw new Exception("Expected four meshes");
                var ears = renderers.Single(r => r.name == "SparkEars");
                ears.SetBlendShapeWeight(ears.sharedMesh.GetBlendShapeIndex("EarDevelopment"), 100);
                var head = renderers.Single(r => r.name == "SparkHead");
                var headBone = instance.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Head");
                var eyes = renderers.Single(r => r.name == "SparkEyes");
                var baked = new Mesh(); eyes.BakeMesh(baked);
                Vector3 center = Vector3.zero;
                foreach (var v in baked.vertices) center += eyes.transform.TransformPoint(v);
                center /= baked.vertexCount;
                UnityEngine.Object.DestroyImmediate(baked);
                float forward = instance.transform.InverseTransformPoint(center).z - instance.transform.InverseTransformPoint(headBone.position).z;
                report.AppendLine("Facing offset +Z=" + forward);
                if (forward < .02f) throw new Exception("Model does not face +Z");
                if (Quaternion.Angle(instance.transform.localRotation, Quaternion.identity) > .01f || (instance.transform.localScale - Vector3.one).magnitude > .001f) throw new Exception("Unexpected root transform");
                int shapes = renderers.Sum(r => r.sharedMesh.blendShapeCount);
                if (shapes != 3) throw new Exception("Expected three shapes");
                var vertices = new Vector3[head.sharedMesh.vertexCount];
                var normals = new Vector3[vertices.Length];
                var tangents = new Vector3[vertices.Length];
                int protectedSlot = Array.FindIndex(head.sharedMaterials, m => m == map["Antennas_Material"]); report.AppendLine("Head materials: " + string.Join(",", head.sharedMaterials.Select(m => m.name)) + " protectedSlot=" + protectedSlot); var protectedVertices = new HashSet<int>(head.sharedMesh.GetTriangles(protectedSlot));
                foreach (string shape in new[] { "Sad", "Angry" })
                {
                    head.sharedMesh.GetBlendShapeFrameVertices(head.sharedMesh.GetBlendShapeIndex(shape), 0, vertices, normals, tangents);
                    float delta = protectedVertices.Max(i => vertices[i].magnitude);
                    float normal = protectedVertices.Max(i => normals[i].magnitude);
                    report.AppendLine(shape + " scales/antennae vertex delta=" + delta + " normal delta=" + normal);
                    if (delta > .00001f || normal > .0001f) throw new Exception("Scale changes under " + shape + " position=" + delta + " normal=" + normal);
                }
                foreach (var clip in importedClips)
                {
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                        if (!string.IsNullOrEmpty(binding.path) && instance.transform.Find(binding.path) == null) throw new Exception("Unresolved animation binding " + clip.name + ": " + binding.path);
                }
                string prefab = Root + "/Spark_01.prefab";
                if (File.Exists(prefab))
                {
                    report.AppendLine("Existing prefab kept: " + prefab);
                }
                else
                {
                    PrefabUtility.SaveAsPrefabAsset(instance, prefab);
                    report.AppendLine("Prefab created: " + prefab);
                }
                report.AppendLine("PASS: 14 clips; 4 meshes; 3 shapes; +Z; static pose; material remaps; scale shape normals; valid animation paths.");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }
    }
}
