using System.Collections.Generic;
using UnityEngine;

namespace Codex.MixamoAttachments.Editor
{
    internal readonly struct SocketDefinition
    {
        public readonly AttachmentType Type;
        public readonly string DisplayName;
        public readonly string SocketName;
        public readonly HumanBodyBones HumanBone;
        public readonly string MixamoBoneName;

        public string SocketId => Type.ToString();

        public SocketDefinition(
            AttachmentType type,
            string displayName,
            string socketName,
            HumanBodyBones humanBone,
            string mixamoBoneName)
        {
            Type = type;
            DisplayName = displayName;
            SocketName = socketName;
            HumanBone = humanBone;
            MixamoBoneName = mixamoBoneName;
        }

        public static readonly IReadOnlyList<SocketDefinition> All = new[]
        {
            new SocketDefinition(AttachmentType.RightHandWeapon, "右手武器", "Socket_RightHandWeapon", HumanBodyBones.RightHand, "RightHand"),
            new SocketDefinition(AttachmentType.LeftHandWeapon, "左手武器", "Socket_LeftHandWeapon", HumanBodyBones.LeftHand, "LeftHand"),
            new SocketDefinition(AttachmentType.TwoHandWeapon, "双手武器", "Socket_TwoHandWeapon", HumanBodyBones.RightHand, "RightHand"),
            new SocketDefinition(AttachmentType.LeftHandShield, "左手盾牌", "Socket_LeftHandShield", HumanBodyBones.LeftHand, "LeftHand"),
            new SocketDefinition(AttachmentType.LeftForearmShield, "左前臂盾牌", "Socket_LeftForearmShield", HumanBodyBones.LeftLowerArm, "LeftForeArm"),
            new SocketDefinition(AttachmentType.RightHandShield, "右手盾牌", "Socket_RightHandShield", HumanBodyBones.RightHand, "RightHand"),
            new SocketDefinition(AttachmentType.NeckAccessory, "项链/颈部附件", "Socket_NeckAccessory", HumanBodyBones.Neck, "Neck"),
            new SocketDefinition(AttachmentType.HeadAccessory, "头部附件", "Socket_HeadAccessory", HumanBodyBones.Head, "Head"),
            new SocketDefinition(AttachmentType.Backpack, "背包", "Socket_Backpack", HumanBodyBones.UpperChest, "Spine2"),
            new SocketDefinition(AttachmentType.BackWeapon, "背部武器", "Socket_BackWeapon", HumanBodyBones.UpperChest, "Spine2"),
            new SocketDefinition(AttachmentType.CapeRoot, "披风根部", "Socket_CapeRoot", HumanBodyBones.UpperChest, "Spine2"),
            new SocketDefinition(AttachmentType.ChestAccessory, "胸前附件", "Socket_ChestAccessory", HumanBodyBones.UpperChest, "Spine2"),
            new SocketDefinition(AttachmentType.Belt, "腰带", "Socket_Belt", HumanBodyBones.Hips, "Hips"),
            new SocketDefinition(AttachmentType.LeftHipAccessory, "左腰附件", "Socket_LeftHip", HumanBodyBones.Hips, "Hips"),
            new SocketDefinition(AttachmentType.RightHipAccessory, "右腰附件", "Socket_RightHip", HumanBodyBones.Hips, "Hips"),
            new SocketDefinition(AttachmentType.RearHipAccessory, "后腰附件", "Socket_RearHip", HumanBodyBones.Hips, "Hips")
        };

        public static bool TryGet(AttachmentType type, out SocketDefinition result)
        {
            foreach (var definition in All)
            {
                if (definition.Type != type)
                    continue;

                result = definition;
                return true;
            }

            result = default;
            return false;
        }
    }
}
