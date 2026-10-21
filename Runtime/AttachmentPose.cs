using System;
using UnityEngine;

namespace Codex.MixamoAttachments
{
    [Serializable]
    public struct AttachmentPose
    {
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale;

        public static AttachmentPose Identity => new AttachmentPose
        {
            localPosition = Vector3.zero,
            localEulerAngles = Vector3.zero,
            localScale = Vector3.one
        };

        public Quaternion Rotation => Quaternion.Euler(localEulerAngles);

        public void Apply(Transform target)
        {
            if (target == null)
                return;

            target.localPosition = localPosition;
            target.localRotation = Rotation;
            target.localScale = localScale;
        }

        public static AttachmentPose Capture(Transform source)
        {
            if (source == null)
                return Identity;

            return new AttachmentPose
            {
                localPosition = source.localPosition,
                localEulerAngles = NormalizeEuler(source.localEulerAngles),
                localScale = source.localScale
            };
        }

        public static AttachmentPose Combine(AttachmentPose basePose, AttachmentPose offset)
        {
            return new AttachmentPose
            {
                localPosition = basePose.localPosition + offset.localPosition,
                localEulerAngles = NormalizeEuler((basePose.Rotation * offset.Rotation).eulerAngles),
                localScale = Vector3.Scale(basePose.localScale, offset.localScale)
            };
        }

        public static AttachmentPose RelativeTo(AttachmentPose basePose, AttachmentPose finalPose)
        {
            return new AttachmentPose
            {
                localPosition = finalPose.localPosition - basePose.localPosition,
                localEulerAngles = NormalizeEuler(
                    (Quaternion.Inverse(basePose.Rotation) * finalPose.Rotation).eulerAngles),
                localScale = Divide(finalPose.localScale, basePose.localScale)
            };
        }

        private static Vector3 Divide(Vector3 value, Vector3 divisor)
        {
            return new Vector3(
                Mathf.Approximately(divisor.x, 0f) ? 1f : value.x / divisor.x,
                Mathf.Approximately(divisor.y, 0f) ? 1f : value.y / divisor.y,
                Mathf.Approximately(divisor.z, 0f) ? 1f : value.z / divisor.z);
        }

        private static Vector3 NormalizeEuler(Vector3 euler)
        {
            return new Vector3(NormalizeAngle(euler.x), NormalizeAngle(euler.y), NormalizeAngle(euler.z));
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
