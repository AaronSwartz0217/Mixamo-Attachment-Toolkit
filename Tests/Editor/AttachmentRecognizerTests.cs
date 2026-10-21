using NUnit.Framework;

namespace Codex.MixamoAttachments.Editor.Tests
{
    public sealed class AttachmentRecognizerTests
    {
        [TestCase("WPN_RH_Sword", AttachmentType.RightHandWeapon)]
        [TestCase("WPN_2H_GreatSword", AttachmentType.TwoHandWeapon)]
        [TestCase("SHIELD_LA_Buckler", AttachmentType.LeftForearmShield)]
        [TestCase("BACKPACK_Leather", AttachmentType.Backpack)]
        [TestCase("HIP_REAR_Pouch", AttachmentType.RearHipAccessory)]
        public void RecognizeName_UsesExpectedPrefix(string objectName, AttachmentType expected)
        {
            var result = AttachmentRecognizer.RecognizeName(objectName);
            Assert.That(result.Type, Is.EqualTo(expected));
            Assert.That(result.Source, Is.EqualTo(RecognitionSource.Name));
        }

        [TestCase("Assets/Attachments/Weapons/TwoHanded/Sword.prefab", AttachmentType.TwoHandWeapon)]
        [TestCase("Assets/Attachments/Shields/Forearm/Buckler.prefab", AttachmentType.LeftForearmShield)]
        [TestCase("Assets/Attachments/HipAccessories/Right/Holster.prefab", AttachmentType.RightHipAccessory)]
        public void RecognizePath_UsesExpectedFolder(string path, AttachmentType expected)
        {
            var result = AttachmentRecognizer.RecognizePath(path);
            Assert.That(result.Type, Is.EqualTo(expected));
            Assert.That(result.Source, Is.EqualTo(RecognitionSource.Folder));
        }

        [Test]
        public void MixamoNameMatcher_IgnoresNamespace()
        {
            Assert.That(MixamoBoneResolver.MatchesMixamoName("mixamorig:RightHand", "RightHand"), Is.True);
            Assert.That(MixamoBoneResolver.MatchesMixamoName("Character|RightHand", "RightHand"), Is.True);
            Assert.That(MixamoBoneResolver.MatchesMixamoName("LeftHand", "RightHand"), Is.False);
        }
    }
}
