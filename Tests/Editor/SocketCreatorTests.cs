using NUnit.Framework;
using UnityEngine;

namespace Codex.MixamoAttachments.Editor.Tests
{
    public sealed class SocketCreatorTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Character");
            var hand = new GameObject("mixamorig:RightHand");
            hand.transform.SetParent(root.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void CreateOrRepair_IsIdempotent()
        {
            Assert.That(SocketDefinition.TryGet(AttachmentType.RightHandWeapon, out var definition), Is.True);

            var first = SocketCreator.CreateOrRepair(root, null, definition);
            var second = SocketCreator.CreateOrRepair(root, null, definition);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));
            Assert.That(root.GetComponentsInChildren<AttachmentSocket>(true).Length, Is.EqualTo(1));
            Assert.That(first.transform.parent.name, Is.EqualTo("mixamorig:RightHand"));
        }
    }
}
