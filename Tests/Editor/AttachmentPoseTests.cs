using NUnit.Framework;
using UnityEngine;

namespace Codex.MixamoAttachments.Editor.Tests
{
    public sealed class AttachmentPoseTests
    {
        [Test]
        public void RelativePose_RecombinesToFinalPose()
        {
            var basePose = new AttachmentPose
            {
                localPosition = new Vector3(1f, 2f, 3f),
                localEulerAngles = new Vector3(10f, 20f, 30f),
                localScale = Vector3.one
            };
            var finalPose = new AttachmentPose
            {
                localPosition = new Vector3(2f, 4f, 6f),
                localEulerAngles = new Vector3(25f, 40f, 55f),
                localScale = new Vector3(1.2f, 1.2f, 1.2f)
            };

            var offset = AttachmentPose.RelativeTo(basePose, finalPose);
            var result = AttachmentPose.Combine(basePose, offset);

            Assert.That(Vector3.Distance(result.localPosition, finalPose.localPosition), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(result.Rotation, finalPose.Rotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(result.localScale, finalPose.localScale), Is.LessThan(0.0001f));
        }
    }
}
