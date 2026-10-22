using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Codex.MixamoAttachments.Editor
{
    internal static class CharacterFbxExporter
    {
        private const string ExporterTypeName = "UnityEditor.Formats.Fbx.Exporter.ModelExporter";

        public static bool IsAvailable => FindExporterType() != null;

        public static bool Export(GameObject characterRoot, string absolutePath, bool includeAttachments,
            out string error)
        {
            error = null;
            var exporterType = FindExporterType();
            if (characterRoot == null || exporterType == null)
            {
                error = "没有场景角色，或尚未安装 Unity 官方 FBX Exporter（com.unity.formats.fbx）。";
                return false;
            }

            var exportMethods = exporterType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "ExportObject" &&
                    method.GetParameters().Length >= 2 &&
                    method.GetParameters()[0].ParameterType == typeof(string))
                .ToArray();
            var exportMethod = exportMethods.FirstOrDefault(method => method.GetParameters().Length == 3) ??
                               exportMethods.FirstOrDefault(method => method.GetParameters().Length == 2);
            if (exportMethod == null)
            {
                error = "当前 FBX Exporter 没有兼容的 ExportObject 接口。";
                return false;
            }

            GameObject exportCopy = null;
            try
            {
                var directory = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                exportCopy = UnityEngine.Object.Instantiate(characterRoot);
                exportCopy.name = characterRoot.name.Replace("_AttachmentSetup", string.Empty);
                exportCopy.hideFlags = HideFlags.HideAndDontSave;

                if (!includeAttachments)
                    foreach (var item in exportCopy.GetComponentsInChildren<AttachmentItem>(true))
                        UnityEngine.Object.DestroyImmediate(item.gameObject);

                var parameters = exportMethod.GetParameters().Length == 3
                    ? new[] { (object)absolutePath, exportCopy, CreateBinaryExportOptions(exportMethod) }
                    : new object[] { absolutePath, exportCopy };
                exportMethod.Invoke(null, parameters);
                if (!File.Exists(absolutePath))
                {
                    error = "FBX Exporter 未生成文件，请检查 Unity Console。";
                    return false;
                }

                AssetDatabase.Refresh();
                ConfigureImporterWhenInsideProject(absolutePath);
                return true;
            }
            catch (TargetInvocationException exception)
            {
                error = exception.InnerException?.Message ?? exception.Message;
                return false;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                if (exportCopy != null) UnityEngine.Object.DestroyImmediate(exportCopy);
            }
        }

        private static Type FindExporterType()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(ExporterTypeName, false))
                .FirstOrDefault(type => type != null);
        }

        private static object CreateBinaryExportOptions(MethodInfo exportMethod)
        {
            var optionsType = exportMethod.GetParameters()[2].ParameterType;
            var options = Activator.CreateInstance(optionsType);
            var formatProperty = optionsType.GetProperty("ExportFormat", BindingFlags.Public | BindingFlags.Instance);
            if (formatProperty == null || !formatProperty.CanWrite || !formatProperty.PropertyType.IsEnum)
                return options;

            var binary = Enum.Parse(formatProperty.PropertyType, "Binary", true);
            formatProperty.SetValue(options, binary);
            return options;
        }

        private static void ConfigureImporterWhenInsideProject(string absolutePath)
        {
            var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var fullPath = Path.GetFullPath(absolutePath);
            if (!fullPath.StartsWith(projectPath + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)) return;

            var assetPath = fullPath.Substring(projectPath.Length + 1).Replace('\\', '/');
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            if (!(AssetImporter.GetAtPath(assetPath) is ModelImporter importer)) return;

            // T-pose-W supplies only the extra hand hierarchy and pose. Never replace
            // the target character's physical dimensions with the reference import scale.
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.globalScale = 1f;
            importer.SaveAndReimport();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
        }
    }
}
