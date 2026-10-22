using System;
using System.Collections.Generic;
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
            bool scaleByHandLength,
            bool preserveReferenceSpaceRotation = true)
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

            created += CopyExtraChain(
                referenceRight, targetRight, "transform1", "joint1", rightScale,
                referenceModel.transform, targetRoot.transform, preserveReferenceSpaceRotation);
            created += CopyExtraChain(
                referenceLeft, targetLeft, "transform2", "joint2", leftScale,
                referenceModel.transform, targetRoot.transform, preserveReferenceSpaceRotation);
            var synchronized = new HashSet<AttachmentType>();
            updated += ApplyReferenceSocketPoses(
                targetRoot, targetAnimator, referenceModel, referenceAnimator,
                rightScale, leftScale, synchronized, preserveReferenceSpaceRotation);
            updated += ApplyJointPoseToSockets(
                targetRoot,
                referenceModel,
                referenceRight,
                targetRight,
                "joint1",
                rightScale,
                preserveReferenceSpaceRotation,
                synchronized,
                AttachmentType.RightHandWeapon,
                AttachmentType.TwoHandWeapon,
                AttachmentType.RightHandShield);
            updated += ApplyJointPoseToSockets(
                targetRoot,
                referenceModel,
                referenceLeft,
                targetLeft,
                "joint2",
                leftScale,
                preserveReferenceSpaceRotation,
                synchronized,
                AttachmentType.LeftHandWeapon,
                AttachmentType.LeftHandShield);

            return new Result(created, updated, rightScale, leftScale);
        }

        public static int EnsureLegacyHandChains(
            GameObject targetRoot,
            Animator targetAnimator)
        {
            if (targetRoot == null)
                return 0;

            var created = 0;
            created += EnsureLegacyHandChain(
                targetRoot,
                ResolveHand(targetRoot, targetAnimator, true),
                "transform1",
                "joint1",
                AttachmentType.RightHandWeapon);
            created += EnsureLegacyHandChain(
                targetRoot,
                ResolveHand(targetRoot, targetAnimator, false),
                "transform2",
                "joint2",
                AttachmentType.LeftHandWeapon);
            return created;
        }

        private static int EnsureLegacyHandChain(
            GameObject targetRoot,
            Transform hand,
            string transformName,
            string jointName,
            AttachmentType sourceSocketType)
        {
            if (hand == null ||
                !SocketDefinition.TryGet(sourceSocketType, out var definition))
                return 0;

            var socket = SocketCreator.FindExisting(targetRoot, definition);
            if (socket == null || socket.transform.parent != hand)
                return 0;

            var created = 0;
            var helper = FindDirectChild(hand, transformName);
            if (helper == null)
            {
                helper = CreateChild(hand, transformName);
                created++;
            }

            Undo.RecordObject(helper, "更新兼容挂点辅助节点");
            helper.localPosition = Vector3.zero;
            helper.localRotation = Quaternion.identity;
            helper.localScale = Vector3.one;
            EditorUtility.SetDirty(helper);

            var joint = FindDirectChild(helper, jointName);
            if (joint == null)
            {
                joint = CreateChild(helper, jointName);
                created++;
            }

            Undo.RecordObject(joint, "更新兼容挂点关节");
            joint.localPosition = socket.transform.localPosition;
            joint.localRotation = socket.transform.localRotation;
            joint.localScale = socket.transform.localScale;
            EditorUtility.SetDirty(joint);
            return created;
        }

        private static int ApplyReferenceSocketPoses(
            GameObject targetRoot,
            Animator targetAnimator,
            GameObject referenceRoot,
            Animator referenceAnimator,
            float rightScale,
            float leftScale,
            HashSet<AttachmentType> synchronized,
            bool preserveReferenceSpaceRotation)
        {
            var updated = 0;
            foreach (var definition in SocketDefinition.All)
            {
                var targetBone = MixamoBoneResolver.Resolve(targetRoot, targetAnimator, definition);
                var referenceBone = MixamoBoneResolver.Resolve(referenceRoot, referenceAnimator, definition);
                var targetSocket = SocketCreator.FindExisting(targetRoot, definition);
                var referenceSocket = FindDirectChild(referenceBone, definition.SocketName);
                if (targetBone == null || referenceSocket == null || targetSocket == null ||
                    targetSocket.transform.parent != targetBone)
                    continue;

                var positionScale = GetSocketPositionScale(definition.Type, rightScale, leftScale);
                Undo.RecordObject(targetSocket.transform, "应用参考模型挂点姿态");
                targetSocket.transform.localPosition = ConvertLocalPosition(
                    referenceSocket,
                    referenceRoot.transform,
                    targetSocket.transform.parent,
                    targetRoot.transform,
                    positionScale,
                    preserveReferenceSpaceRotation);
                targetSocket.transform.localRotation = ConvertLocalRotation(
                    referenceSocket,
                    referenceRoot.transform,
                    targetSocket.transform.parent,
                    targetRoot.transform,
                    preserveReferenceSpaceRotation);
                targetSocket.transform.localScale = referenceSocket.localScale;
                EditorUtility.SetDirty(targetSocket.transform);
                synchronized.Add(definition.Type);
                updated++;
            }

            return updated;
        }

        private static float GetSocketPositionScale(AttachmentType type, float rightScale, float leftScale)
        {
            switch (type)
            {
                case AttachmentType.RightHandWeapon:
                case AttachmentType.RightHandShield:
                case AttachmentType.TwoHandWeapon:
                    return rightScale;
                case AttachmentType.LeftHandWeapon:
                case AttachmentType.LeftHandShield:
                    return leftScale;
                default:
                    return 1f;
            }
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
            float scale,
            Transform referenceRoot,
            Transform targetRoot,
            bool preserveReferenceSpaceRotation)
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

            CopyLocalPose(
                referenceTransform,
                targetTransform,
                scale,
                referenceRoot,
                targetRoot,
                preserveReferenceSpaceRotation);

            var targetJoint = FindDirectChild(targetTransform, jointName);
            if (targetJoint == null)
            {
                targetJoint = CreateChild(targetTransform, jointName);
                created++;
            }

            CopyLocalPose(
                referenceJoint,
                targetJoint,
                scale,
                referenceRoot,
                targetRoot,
                preserveReferenceSpaceRotation);
            return created;
        }

        private static int ApplyJointPoseToSockets(
            GameObject targetRoot,
            GameObject referenceRoot,
            Transform referenceHand,
            Transform targetHand,
            string jointName,
            float scale,
            bool preserveReferenceSpaceRotation,
            HashSet<AttachmentType> synchronized,
            params AttachmentType[] socketTypes)
        {
            if (referenceHand == null || targetHand == null)
                return 0;

            var referenceJoint = FindBySuffix(referenceHand, jointName);
            if (referenceJoint == null)
                return 0;

            var updated = 0;

            foreach (var type in socketTypes)
            {
                if (synchronized.Contains(type))
                    continue;
                if (!SocketDefinition.TryGet(type, out var definition))
                    continue;

                var socket = SocketCreator.FindExisting(targetRoot, definition);
                if (socket == null || socket.transform.parent != targetHand)
                    continue;

                Undo.RecordObject(socket.transform, "应用参考手部挂点姿态");
                socket.transform.localPosition = ConvertOffsetPosition(
                    referenceJoint.position,
                    referenceHand,
                    referenceRoot.transform,
                    socket.transform.parent,
                    targetRoot.transform,
                    scale,
                    preserveReferenceSpaceRotation);
                socket.transform.localRotation = ConvertLocalRotation(
                    referenceJoint,
                    referenceRoot.transform,
                    socket.transform.parent,
                    targetRoot.transform,
                    preserveReferenceSpaceRotation);
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

        private static void CopyLocalPose(
            Transform source,
            Transform target,
            float positionScale,
            Transform sourceRoot,
            Transform targetRoot,
            bool preserveReferenceSpaceRotation)
        {
            Undo.RecordObject(target, "更新参考辅助骨骼");
            target.localPosition = ConvertLocalPosition(
                source,
                sourceRoot,
                target.parent,
                targetRoot,
                positionScale,
                preserveReferenceSpaceRotation);
            target.localRotation = ConvertLocalRotation(
                source,
                sourceRoot,
                target.parent,
                targetRoot,
                preserveReferenceSpaceRotation);
            target.localScale = source.localScale;
            EditorUtility.SetDirty(target);
        }

        private static Vector3 ConvertLocalPosition(
            Transform source,
            Transform sourceRoot,
            Transform targetParent,
            Transform targetRoot,
            float positionScale,
            bool preserveReferenceSpacePose)
        {
            if (source == null || source.parent == null)
                return Vector3.zero;

            return ConvertOffsetPosition(
                source.position,
                source.parent,
                sourceRoot,
                targetParent,
                targetRoot,
                positionScale,
                preserveReferenceSpacePose);
        }

        private static Vector3 ConvertOffsetPosition(
            Vector3 sourceWorldPosition,
            Transform sourceParent,
            Transform sourceRoot,
            Transform targetParent,
            Transform targetRoot,
            float positionScale,
            bool preserveReferenceSpacePose)
        {
            if (sourceParent == null || targetParent == null)
                return Vector3.zero;

            if (!preserveReferenceSpacePose || sourceRoot == null || targetRoot == null)
                return sourceParent.InverseTransformPoint(sourceWorldPosition) * positionScale;

            // Position offsets, like rotations, are expressed in the parent bone's axes.
            // Convert the offset through character space before applying it to a rig whose
            // hand/bone basis can be different. TransformVector also compensates parent
            // scale, so helper joints and their matching sockets stay coincident.
            var sourceWorldOffset = sourceWorldPosition - sourceParent.position;
            var sourceRootOffset = sourceRoot.InverseTransformVector(sourceWorldOffset) * positionScale;
            var targetWorldOffset = targetRoot.TransformVector(sourceRootOffset);
            return targetParent.InverseTransformVector(targetWorldOffset);
        }

        private static Quaternion ConvertLocalRotation(
            Transform source,
            Transform sourceRoot,
            Transform targetParent,
            Transform targetRoot,
            bool preserveReferenceSpaceRotation)
        {
            if (!preserveReferenceSpaceRotation ||
                source == null || sourceRoot == null || targetParent == null || targetRoot == null)
                return source != null ? source.localRotation : Quaternion.identity;

            // localRotation only has meaning in the source bone's coordinate system. Mixamo
            // characters can use very different hand bases, so copying it verbatim visibly
            // rotates weapons. Convert through each character root to preserve the reference
            // socket's character-space axes on the target rig.
            var sourceRootRotation = Quaternion.Inverse(sourceRoot.rotation) * source.rotation;
            var targetParentRootRotation = Quaternion.Inverse(targetRoot.rotation) * targetParent.rotation;
            return Quaternion.Inverse(targetParentRootRotation) * sourceRootRotation;
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
