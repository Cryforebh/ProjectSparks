using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ProjectSparks.SparkPreview.Editor
{
    public static class SparkImportAudit
    {
        public static void Run()
        {
            const string folder = "Assets/_Project/Art/SparkPreview/Source";
            var report = new StringBuilder();
            foreach (string file in Directory.GetFiles(folder, "*.fbx"))
            {
                string path = file.Replace('\\', '/');
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = true;
                importer.bakeAxisConversion = true;
                importer.optimizeGameObjects = false;
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                importer.importBlendShapes = true;
                importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                report.AppendLine(path);
                foreach (var t in model.GetComponentsInChildren<Transform>(true))
                    report.AppendLine($"T {AnimationUtility.CalculateTransformPath(t, model.transform)} p={t.localPosition:F6} q={t.localRotation:F6} s={t.localScale:F6}");
                foreach (var c in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
                    report.AppendLine($"CLIP {c.name} length={c.length} fps={c.frameRate} bindings={AnimationUtility.GetCurveBindings(c).Length}");
                foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    report.AppendLine($"MESH {r.name} vertices={r.sharedMesh.vertexCount} shapes={r.sharedMesh.blendShapeCount} materials={string.Join(",", r.sharedMaterials.Select(m => m ? m.name : "NULL"))}");
                    for (int i = 0; i < r.sharedMesh.blendShapeCount; i++) report.AppendLine("SHAPE " + r.sharedMesh.GetBlendShapeName(i));
                }
            }
            Directory.CreateDirectory("H:/Diplom/Verification/SparkPreview");
            File.WriteAllText("H:/Diplom/Verification/SparkPreview/ImportAudit.txt", report.ToString());
        }
    }
}
