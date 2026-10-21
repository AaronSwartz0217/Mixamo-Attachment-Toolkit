using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Codex.MixamoAttachments.Editor
{
    internal static class ReferenceModelLocator
    {
        private static readonly string[] PreferredNames =
        {
            "Human_WithReferenceBones",
            "T-pose-W"
        };

        public static GameObject FindDefault()
        {
            foreach (var preferredName in PreferredNames)
            {
                var guids = AssetDatabase.FindAssets(preferredName + " t:GameObject");
                Array.Sort(guids, StringComparer.Ordinal);
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.Equals(Path.GetFileNameWithoutExtension(path), preferredName,
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (model != null)
                        return model;
                }
            }

            return null;
        }
    }
}
