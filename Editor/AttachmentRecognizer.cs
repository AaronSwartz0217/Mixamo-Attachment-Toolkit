using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codex.MixamoAttachments.Editor
{
    internal enum RecognitionSource
    {
        None,
        Component,
        Folder,
        Name
    }

    internal readonly struct RecognitionResult
    {
        public readonly AttachmentType Type;
        public readonly RecognitionSource Source;
        public readonly string Evidence;

        public bool IsRecognized => Type != AttachmentType.None;

        public RecognitionResult(AttachmentType type, RecognitionSource source, string evidence)
        {
            Type = type;
            Source = source;
            Evidence = evidence;
        }
    }

    internal static class AttachmentRecognizer
    {
        private static readonly KeyValuePair<string, AttachmentType>[] PrefixRules =
        {
            Rule("HIP_REAR_", AttachmentType.RearHipAccessory),
            Rule("SHIELD_LA_", AttachmentType.LeftForearmShield),
            Rule("SHIELD_LH_", AttachmentType.LeftHandShield),
            Rule("SHIELD_RH_", AttachmentType.RightHandShield),
            Rule("WPN_BACK_", AttachmentType.BackWeapon),
            Rule("WPN_2H_", AttachmentType.TwoHandWeapon),
            Rule("WPN_RH_", AttachmentType.RightHandWeapon),
            Rule("WPN_LH_", AttachmentType.LeftHandWeapon),
            Rule("BACKPACK_", AttachmentType.Backpack),
            Rule("CAPE_", AttachmentType.CapeRoot),
            Rule("CHEST_", AttachmentType.ChestAccessory),
            Rule("BELT_", AttachmentType.Belt),
            Rule("HIP_L_", AttachmentType.LeftHipAccessory),
            Rule("HIP_R_", AttachmentType.RightHipAccessory),
            Rule("NECK_", AttachmentType.NeckAccessory),
            Rule("HEAD_", AttachmentType.HeadAccessory)
        };

        public static RecognitionResult Recognize(GameObject item, string assetPath)
        {
            if (item == null)
                return default;

            var marker = item.GetComponent<AttachmentItem>();
            if (marker != null && marker.Type != AttachmentType.None)
                return new RecognitionResult(marker.Type, RecognitionSource.Component, "AttachmentItem");

            var folderResult = RecognizePath(assetPath);
            if (folderResult.IsRecognized)
                return folderResult;

            return RecognizeName(item.name);
        }

        internal static RecognitionResult RecognizeName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return default;

            foreach (var rule in PrefixRules)
            {
                if (objectName.StartsWith(rule.Key, StringComparison.OrdinalIgnoreCase))
                    return new RecognitionResult(rule.Value, RecognitionSource.Name, rule.Key);
            }

            return default;
        }

        internal static RecognitionResult RecognizePath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return default;

            var path = assetPath.Replace('\\', '/').ToLowerInvariant();

            if (path.Contains("/weapons/twohanded/"))
                return Folder(AttachmentType.TwoHandWeapon, "Weapons/TwoHanded");
            if (path.Contains("/weapons/righthand/"))
                return Folder(AttachmentType.RightHandWeapon, "Weapons/RightHand");
            if (path.Contains("/weapons/lefthand/"))
                return Folder(AttachmentType.LeftHandWeapon, "Weapons/LeftHand");
            if (path.Contains("/weapons/back/"))
                return Folder(AttachmentType.BackWeapon, "Weapons/Back");
            if (path.Contains("/shields/forearm/"))
                return Folder(AttachmentType.LeftForearmShield, "Shields/Forearm");
            if (path.Contains("/shields/hand/"))
                return Folder(AttachmentType.LeftHandShield, "Shields/Hand");
            if (path.Contains("/backpacks/"))
                return Folder(AttachmentType.Backpack, "Backpacks");
            if (path.Contains("/capes/"))
                return Folder(AttachmentType.CapeRoot, "Capes");
            if (path.Contains("/neck/"))
                return Folder(AttachmentType.NeckAccessory, "Neck");
            if (path.Contains("/head/"))
                return Folder(AttachmentType.HeadAccessory, "Head");
            if (path.Contains("/belts/"))
                return Folder(AttachmentType.Belt, "Belts");
            if (path.Contains("/hipaccessories/left/"))
                return Folder(AttachmentType.LeftHipAccessory, "HipAccessories/Left");
            if (path.Contains("/hipaccessories/right/"))
                return Folder(AttachmentType.RightHipAccessory, "HipAccessories/Right");
            if (path.Contains("/hipaccessories/rear/"))
                return Folder(AttachmentType.RearHipAccessory, "HipAccessories/Rear");
            if (path.Contains("/chest/"))
                return Folder(AttachmentType.ChestAccessory, "Chest");

            return default;
        }

        private static RecognitionResult Folder(AttachmentType type, string evidence)
        {
            return new RecognitionResult(type, RecognitionSource.Folder, evidence);
        }

        private static KeyValuePair<string, AttachmentType> Rule(string prefix, AttachmentType type)
        {
            return new KeyValuePair<string, AttachmentType>(prefix, type);
        }
    }
}
