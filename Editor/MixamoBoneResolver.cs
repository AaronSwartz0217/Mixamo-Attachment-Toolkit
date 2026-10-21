using System;
using UnityEngine;

namespace Codex.MixamoAttachments.Editor
{
    internal static class MixamoBoneResolver
    {
        public static Animator FindAnimator(GameObject characterRoot)
        {
            return characterRoot == null ? null : characterRoot.GetComponentInChildren<Animator>(true);
        }

        public static Transform Resolve(GameObject characterRoot, Animator animator, SocketDefinition definition)
        {
            if (animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid)
            {
                var humanoidBone = animator.GetBoneTransform(definition.HumanBone);
                if (humanoidBone != null)
                    return humanoidBone;

                if (definition.HumanBone == HumanBodyBones.UpperChest)
                {
                    humanoidBone = animator.GetBoneTransform(HumanBodyBones.Chest);
                    if (humanoidBone != null)
                        return humanoidBone;
                }
            }

            if (characterRoot == null)
                return null;

            var transforms = characterRoot.GetComponentsInChildren<Transform>(true);
            foreach (var candidate in transforms)
            {
                if (MatchesMixamoName(candidate.name, definition.MixamoBoneName))
                    return candidate;
            }

            if (definition.HumanBone == HumanBodyBones.UpperChest)
            {
                foreach (var fallback in new[] { "Spine1", "Chest" })
                {
                    foreach (var candidate in transforms)
                    {
                        if (MatchesMixamoName(candidate.name, fallback))
                            return candidate;
                    }
                }
            }

            return null;
        }

        internal static bool MatchesMixamoName(string candidateName, string expectedSuffix)
        {
            if (string.IsNullOrWhiteSpace(candidateName) || string.IsNullOrWhiteSpace(expectedSuffix))
                return false;

            if (string.Equals(candidateName, expectedSuffix, StringComparison.OrdinalIgnoreCase))
                return true;

            var normalized = candidateName.Replace("|", ":").Replace("/", ":");
            var separator = normalized.LastIndexOf(':');
            var suffix = separator >= 0 ? normalized.Substring(separator + 1) : normalized;
            return string.Equals(suffix, expectedSuffix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
