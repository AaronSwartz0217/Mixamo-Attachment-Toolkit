using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Codex.MixamoAttachments.Editor
{
    internal static class FbxExportSmokeTest
    {
        private const string RequestFile = "Temp/CodexRunFbxExportSmokeTest.request";
        private const string SourcePath = "Assets/Animate/TPOSE/备份测试/Human.fbx";
        private const string ReferencePath = "Assets/Animate/导入的tpose参考/T-pose-W.fbx";
        private const string OutputFolder = "Assets/Animate/TPOSE/FBX导出测试";
        private const string OutputPath = OutputFolder + "/Human_WithReferenceBones.fbx";
        private const string ReportPath = OutputFolder + "/测试报告.txt";

        [InitializeOnLoadMethod]
        private static void RunRequestedTestAfterReload()
        {
            var absoluteRequest = Path.Combine(Directory.GetCurrentDirectory(), RequestFile);
            if (!File.Exists(absoluteRequest)) return;
            File.Delete(absoluteRequest);
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Tools/Mixamo Attachment Toolkit/Tests/导出 Human FBX 测试")]
        public static void Run()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            var reference = AssetDatabase.LoadAssetAtPath<GameObject>(ReferencePath);
            if (source == null || reference == null)
            {
                Debug.LogError($"[FBX Export Test] 缺少源模型或参考模型。Source={source != null}, Reference={reference != null}");
                return;
            }

            EnsureOutputFolder();
            GameObject working = null;
            GameObject importedInstance = null;
            try
            {
                working = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (working == null) working = UnityEngine.Object.Instantiate(source);
                working.name = "Human_AttachmentSetup";
                var animator = MixamoBoneResolver.FindAnimator(working);
                var sourceHeight = CalculateHeight(working);

                foreach (var definition in SocketDefinition.All)
                    SocketCreator.CreateOrRepair(working, animator, definition);

                var copyResult = ReferenceHandRigCopier.Apply(working, animator, reference, true);
                var absoluteOutput = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), OutputPath));
                if (!CharacterFbxExporter.Export(working, absoluteOutput, false, out var exportError))
                    throw new InvalidOperationException(exportError);

                var imported = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPath);
                if (imported == null) throw new InvalidOperationException("导出的 FBX 无法重新载入。 ");
                importedInstance = PrefabUtility.InstantiatePrefab(imported) as GameObject;
                if (importedInstance == null) importedInstance = UnityEngine.Object.Instantiate(imported);

                var joint1 = FindRecursive(importedInstance.transform, "joint1");
                var joint2 = FindRecursive(importedInstance.transform, "joint2");
                var meshCount = importedInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
                var exportedHeight = CalculateHeight(importedInstance);
                var heightRatio = sourceHeight > 0.0001f ? exportedHeight / sourceHeight : 0f;
                var importer = AssetImporter.GetAtPath(OutputPath) as ModelImporter;
                var humanoid = importer != null && importer.animationType == ModelImporterAnimationType.Human;
                var passed = joint1 != null && joint2 != null && meshCount > 0 && humanoid &&
                             heightRatio > 0.98f && heightRatio < 1.02f;

                var report =
                    $"FBX Export Smoke Test\r\n" +
                    $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n" +
                    $"Result: {(passed ? "PASS" : "FAIL")}\r\n" +
                    $"Source: {SourcePath}\r\n" +
                    $"Reference: {ReferencePath}\r\n" +
                    $"Output: {OutputPath}\r\n" +
                    $"Created reference bones: {copyResult.CreatedBones}\r\n" +
                    $"joint1: {joint1 != null}\r\n" +
                    $"joint2: {joint2 != null}\r\n" +
                    $"Skinned meshes: {meshCount}\r\n" +
                    $"Humanoid importer: {humanoid}\r\n" +
                    $"Source height: {sourceHeight:F6}\r\n" +
                    $"Exported height: {exportedHeight:F6}\r\n" +
                    $"Height ratio: {heightRatio:F6}\r\n";
                File.WriteAllText(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ReportPath)), report);
                AssetDatabase.Refresh();

                if (passed)
                    Debug.Log($"[FBX Export Test] PASS\n{report}", imported);
                else
                    Debug.LogError($"[FBX Export Test] FAIL\n{report}", imported);
                Selection.activeObject = imported;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (working != null) UnityEngine.Object.DestroyImmediate(working);
                if (importedInstance != null) UnityEngine.Object.DestroyImmediate(importedInstance);
            }
        }

        private static void EnsureOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/Animate/TPOSE", "FBX导出测试");
        }

        private static float CalculateHeight(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return 0f;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds.size.y;
        }

        private static Transform FindRecursive(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
