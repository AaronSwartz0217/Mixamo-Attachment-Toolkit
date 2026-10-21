using NUnit.Framework;
using UnityEngine;

namespace Codex.MixamoAttachments.Editor.Tests
{
    public sealed class ReferenceHandRigCopierTests
    {
        private GameObject target;
        private GameObject reference;

        [SetUp]
        public void SetUp()
        {
            target = CreateHands("Target", 0.20f, false);
            reference = CreateHands("Reference", 0.10f, true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(reference);
        }

        [Test]
        public void Apply_CopiesAuxiliaryHierarchyAndScalesSocketPose()
        {
            Assert.That(SocketDefinition.TryGet(AttachmentType.RightHandWeapon, out var right), Is.True);
            Assert.That(SocketDefinition.TryGet(AttachmentType.LeftHandWeapon, out var left), Is.True);
            var rightSocket = SocketCreator.CreateOrRepair(target, null, right);
            var leftSocket = SocketCreator.CreateOrRepair(target, null, left);

            var result = ReferenceHandRigCopier.Apply(target, null, reference, true);

            Assert.That(result.CreatedBones, Is.EqualTo(4));
            Assert.That(result.UpdatedSockets, Is.EqualTo(2));
            Assert.That(result.RightScale, Is.EqualTo(2f).Within(0.001f));
            Assert.That(result.LeftScale, Is.EqualTo(2f).Within(0.001f));
            Assert.That(Find(target.transform, "transform1/joint1"), Is.Not.Null);
            Assert.That(Find(target.transform, "transform2/joint2"), Is.Not.Null);
            Assert.That(rightSocket.transform.localPosition, Is.EqualTo(new Vector3(0.02f, 0.10f, 0.04f)));
            Assert.That(leftSocket.transform.localPosition, Is.EqualTo(new Vector3(-0.02f, 0.10f, 0.04f)));
        }

        [Test]
        public void Apply_IsIdempotent()
        {
            ReferenceHandRigCopier.Apply(target, null, reference, true);
            var second = ReferenceHandRigCopier.Apply(target, null, reference, true);

            Assert.That(second.CreatedBones, Is.Zero);
            Assert.That(FindAllByName(target.transform, "joint1"), Is.EqualTo(1));
            Assert.That(FindAllByName(target.transform, "joint2"), Is.EqualTo(1));
        }

        [Test]
        public void Apply_PrefersReferenceSocketPoseAndScalesHandPosition()
        {
            Assert.That(SocketDefinition.TryGet(AttachmentType.RightHandWeapon, out var definition), Is.True);
            var targetSocket = SocketCreator.CreateOrRepair(target, null, definition);
            var referenceHand = reference.transform.Find("mixamorig:RightHand");
            var referenceSocket = new GameObject(definition.SocketName).transform;
            referenceSocket.SetParent(referenceHand, false);
            referenceSocket.localPosition = new Vector3(0.01f, 0.03f, -0.02f);
            referenceSocket.localRotation = Quaternion.Euler(15f, 25f, 35f);
            referenceSocket.localScale = new Vector3(1.1f, 1.2f, 1.3f);

            ReferenceHandRigCopier.Apply(target, null, reference, true);

            Assert.That(targetSocket.transform.localPosition,
                Is.EqualTo(new Vector3(0.02f, 0.06f, -0.04f)));
            Assert.That(Quaternion.Angle(targetSocket.transform.localRotation, referenceSocket.localRotation),
                Is.LessThan(0.001f));
            Assert.That(targetSocket.transform.localScale, Is.EqualTo(referenceSocket.localScale));
        }

        private static GameObject CreateHands(string rootName, float fingerLength, bool includeReferenceJoints)
        {
            var root = new GameObject(rootName);
            CreateHand(root.transform, "RightHand", "RightHandMiddle1", fingerLength, includeReferenceJoints,
                "transform1", "joint1", new Vector3(0.01f, 0.05f, 0.02f));
            CreateHand(root.transform, "LeftHand", "LeftHandMiddle1", fingerLength, includeReferenceJoints,
                "transform2", "joint2", new Vector3(-0.01f, 0.05f, 0.02f));
            return root;
        }

        private static void CreateHand(
            Transform root,
            string handName,
            string fingerName,
            float fingerLength,
            bool includeJoint,
            string transformName,
            string jointName,
            Vector3 jointPosition)
        {
            var hand = new GameObject("mixamorig:" + handName).transform;
            hand.SetParent(root, false);
            var finger = new GameObject("mixamorig:" + fingerName).transform;
            finger.SetParent(hand, false);
            finger.localPosition = new Vector3(0f, fingerLength, 0f);
            if (!includeJoint)
                return;

            var helper = new GameObject(transformName).transform;
            helper.SetParent(hand, false);
            var joint = new GameObject(jointName).transform;
            joint.SetParent(helper, false);
            joint.localPosition = jointPosition;
            joint.localRotation = Quaternion.Euler(5f, 10f, 15f);
        }

        private static Transform Find(Transform root, string path)
        {
            foreach (var handName in new[] { "mixamorig:RightHand", "mixamorig:LeftHand" })
            {
                var hand = root.Find(handName);
                var found = hand != null ? hand.Find(path) : null;
                if (found != null)
                    return found;
            }
            return null;
        }

        private static int FindAllByName(Transform root, string name)
        {
            var count = 0;
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name == name)
                    count++;
            }
            return count;
        }
    }
}
