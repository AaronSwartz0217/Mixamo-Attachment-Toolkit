using System;
using UnityEditor;
using UnityEngine;

namespace Codex.MixamoAttachments.Editor
{
    internal static class ReferenceHandRigCopier
    {
        internal readonly struct Result
        {
            public readonly int CreatedBones;
            public readonly int UpdatedSockets;
            public readonly float RightScale;
            public readonly float LeftScale;

            public Result(int createdBones, int updatedSockets, float rightScale, float leftScale)
            {
                CreatedBones = createdBones;
                UpdatedSockets = updatedSockets;
                RightScale = rightScale;
                LeftScale = leftScale;
            }
        }

        public static Result Apply(
            GameObject targetRoot,
            Animator targetAnimator,
            GameObject referenceModel,
            bool scaleByHandLength)
        {
            if (targetRoot == null || referenceModel == null)
                return default;

            var referenceAnimator = MixamoBoneResolver.FindAnimator(referenceModel);
            var targetRight = ResolveHand(targetRoot, targetAnimator, true);
            var targetLeft = ResolveHand(targetRoot, targetAnimator, false);
            var referenceRight = ResolveHand(referenceModel, referenceAnimator, true);
            var referenceLeft = ResolveHand(referenceModel, referenceAnimator, false);

            LogMissingReference(referenceRight, referenceLeft, targetRight, targetLeft, referenceModel);

            var rightScale = CalculateScale(referenceRight, targetRight, true, scaleByHandLength);
            var leftScale = CalculateScale(referenceLeft, targetLeft, false, scaleByHandLength);
            var created = 0;
            var updated = 0;

            created += CopyExtraChain(referenceRight, targetRight, "transform1", "joint1", rightScale);
            created += CopyExtraChain(referenceLeft, targetLeft, "transform2", "joint2", leftScale);
            updated += ApplyJointPoseToSockets(
                targetRoot,
                referenceRight,
                targetRight,
                "joint1",
                rightScale,
                AttachmentType.RightHandWeapon,
                AttachmentType.TwoHandWeapon,
                AttachmentType.RightHandShield);
            updated += ApplyJointPoseToSockets(
                targetRoot,
                referenceLeft,
                targetLeft,
                "joint2",
                leftScale,
                AttachmentType.LeftHandWeapon,
                AttachmentType.LeftHandShield);

            return new Result(created, updated, rightScale, leftScale);
        }

        private static Transform ResolveHand(GameObject root, Animator animator, bool right)
        {
            var type = right ? AttachmentType.RightHandWeapon : AttachmentType.LeftHandWeapon;
            return SocketDefinition.TryGet(type, out var definition)
                ? MixamoBoneResolver.Resolve(root, animator, definition)
                : null;
        }

        private static float CalculateScale(
            Transform referenceHand,
            Transform targetHand,
            bool right,
            bool enabled)
        {
            if (!enabled || referenceHand == null || targetHand == null)
                return 1f;

            var fingerName = right ? "RightHandMiddle1" : "LeftHandMiddle1";
            var referenceFinger = FindBySuffix(referenceHand, fingerName);
            var targetFinger = FindBySuffix(targetHand, fingerName);
            if (referenceFinger == null || targetFinger == null)
                return 1f;

            var referenceLength = referenceFinger.localPosition.magnitude;
            var targetLength = targetFinger.localPosition.magnitude;
            if (referenceLength < 0.00001f || targetLength < 0.00001f)
                return 1f;

            return Mathf.Clamp(targetLength / referenceLength, 0.25f, 4f);
        }

        private static int CopyExtraChain(
            Transform referenceHand,
            Transform targetHand,
            string transformName,
            string jointName,
            float scale)
        {
            if (referenceHand == null || targetHand == null)
                return 0;

            var referenceTransform = FindBySuffix(referenceHand, transformName);
            var referenceJoint = referenceTransform != null
                ? FindBySuffix(referenceTransform, jointName)
                : null;
            if (referenceTransform == null || referenceJoint == null)
            {
                Debug.LogWarning(
                    $"[Mixamo Attachments] 参考模型中没有找到 {transformName}/{jointName}，" +
                    "未创建对应辅助骨骼。",
                    referenceHand);
                return 0;
            }

            var created = 0;
            var targetTransform = FindDirectChild(targetHand, transformName);
            if (targetTransform == null)
            {
                targetTransform = CreateChild(targetHand, transformName);
                created++;
            }

            CopyLocalPose(referenceTransform, targetTransform, scale);

            var targetJoint = FindDirectChild(targetTransform, jointName);
            if (targetJoint == null)
            {
                targetJoint = CreateChild(targetTransform, jointName);
                created++;
            }

            CopyLocalPose(referenceJoint, targetJoint, scale);
            return created;
        }

        private static int ApplyJointPoseToSockets(
            GameObject targetRoot,
            Transform referenceHand,
            Transform targetHand,
            string jointName,
            float scale,
            params AttachmentType[] socketTypes)
        {
            if (referenceHand == null || targetHand == null)
                return 0;

            var referenceJoint = FindBySuffix(referenceHand, jointName);
            if (referenceJoint == null)
                return 0;

            var referencePosition = referenceHand.InverseTransformPoint(referenceJoint.position) * scale;
            var referenceRotation = Quaternion.Inverse(referenceHand.rotation) * referenceJoint.rotation;
            var updated = 0;

            foreach (var type in socketTypes)
            {
                if (!SocketDefinition.TryGet(type, out var definition))
                    continue;

                var socket = SocketCreator.FindExisting(targetRoot, definition);
                if (socket == null || socket.transform.parent != targetHand)
                    continue;

                Undo.RecordObject(socket.transform, "应用参考手部挂点姿态");
                socket.transform.localPosition = referencePosition;
                socket.transform.localRotation = referenceRotation;
                socket.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(socket.transform);
                updated++;
            }

            return updated;
        }

        private static Transform CreateChild(Transform parent, string childName)
        {
            var child = new GameObject(childName).transform;
            Undo.RegisterCreatedObjectUndo(child.gameObject, $"创建参考辅助骨骼 {childName}");
            child.SetParent(parent, false);
            return child;
        }

        private static void CopyLocalPose(Transform source, Transform target, float positionScale)
        {
            Undo.RecordObject(target, "更新参考辅助骨骼");
            target.localPosition = source.localPosition * positionScale;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
            EditorUtility.SetDirty(target);
        }

        private static Transform FindDirectChild(Transform parent, string expectedName)
        {
            if (parent == null)
                return null;

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (string.Equals(child.name, expectedName, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        private static Transform FindBySuffix(Transform root, string suffix)
        {
            if (root == null)
                return null;

            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var candidate in transforms)
            {
                var name = candidate.name;
                var separator = name.LastIndexOf(':');
                var cleanName = separator >= 0 ? name.Substring(separator + 1) : name;
                if (string.Equals(cleanName, suffix, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return null;
        }

        private static void LogMissingReference(
            Transform referenceRight,
            Transform referenceLeft,
            Transform targetRight,
            Transform targetLeft,
            GameObject referenceModel)
        {
            if (referenceRight == null || referenceLeft == null)
            {
                Debug.LogWarning(
                    "[Mixamo Attachments] 参考模型没有完整识别到左右手骨骼。" +
                    $" RightHand={(referenceRight != null)}, LeftHand={(referenceLeft != null)}",
                    referenceModel);
            }

            if (targetRight == null || targetLeft == null)
            {
                Debug.LogWarning(
                    "[Mixamo Attachments] 目标角色没有完整识别到左右手骨骼。" +
                    $" RightHand={(targetRight != null)}, LeftHand={(targetLeft != null)}");
            }

            if (referenceRight != null && FindBySuffix(referenceRight, "RightHandMiddle1") == null)
                Debug.LogWarning("[Mixamo Attachments] 参考模型缺少 RightHandMiddle1，右手比例回退为 1。", referenceModel);
            if (referenceLeft != null && FindBySuffix(referenceLeft, "LeftHandMiddle1") == null)
                Debug.LogWarning("[Mixamo Attachments] 参考模型缺少 LeftHandMiddle1，左手比例回退为 1。", referenceModel);
            if (targetRight != null && FindBySuffix(targetRight, "RightHandMiddle1") == null)
                Debug.LogWarning("[Mixamo Attachments] 目标角色缺少 RightHandMiddle1，右手比例回退为 1。", targetRight);
            if (targetLeft != null && FindBySuffix(targetLeft, "LeftHandMiddle1") == null)
                Debug.LogWarning("[Mixamo Attachments] 目标角色缺少 LeftHandMiddle1，左手比例回退为 1。", targetLeft);
        }
    }
}
