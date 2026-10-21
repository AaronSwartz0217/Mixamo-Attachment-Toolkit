using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codex.MixamoAttachments
{
    [CreateAssetMenu(fileName = "WeaponAttachmentProfile", menuName = "Mixamo Attachments/Weapon Profile")]
    public sealed class WeaponAttachmentProfile : ScriptableObject
    {
        [Serializable]
        public sealed class StateOffset
        {
            public AttachmentState state;
            public AttachmentPose offset = default;
        }

        [SerializeField] private string weaponId;
        [SerializeField] private GameObject weaponPrefab;
        [SerializeField] private AttachmentType attachmentType = AttachmentType.RightHandWeapon;
        [SerializeField] private AttachmentPose basePose = default;
        [SerializeField, Min(0f)] private float transitionSpeed = 10f;
        [SerializeField] private List<StateOffset> stateOffsets = new List<StateOffset>();

        public string WeaponId => weaponId;
        public GameObject WeaponPrefab => weaponPrefab;
        public AttachmentType AttachmentType => attachmentType;
        public AttachmentPose BasePose => EnsureScale(basePose);
        public float TransitionSpeed => transitionSpeed;

        public AttachmentPose Evaluate(AttachmentState state)
        {
            var baseValue = BasePose;
            if (state == AttachmentState.Default)
                return baseValue;

            foreach (var entry in stateOffsets)
            {
                if (entry != null && entry.state == state)
                    return AttachmentPose.Combine(baseValue, EnsureScale(entry.offset));
            }

            return baseValue;
        }

        public void Configure(GameObject prefab, AttachmentType type)
        {
            weaponPrefab = prefab;
            attachmentType = type;
            if (string.IsNullOrWhiteSpace(weaponId) && prefab != null)
                weaponId = prefab.name;
            basePose = EnsureScale(basePose);
        }

        public void SetBasePose(AttachmentPose pose)
        {
            basePose = EnsureScale(pose);
        }

        public void SetStateFromFinalPose(AttachmentState state, AttachmentPose finalPose)
        {
            if (state == AttachmentState.Default)
            {
                SetBasePose(finalPose);
                return;
            }

            var offset = AttachmentPose.RelativeTo(BasePose, finalPose);
            var entry = stateOffsets.Find(value => value != null && value.state == state);
            if (entry == null)
            {
                entry = new StateOffset { state = state };
                stateOffsets.Add(entry);
            }

            entry.offset = offset;
        }

        private static AttachmentPose EnsureScale(AttachmentPose pose)
        {
            if (pose.localScale == Vector3.zero)
                pose.localScale = Vector3.one;
            return pose;
        }
    }
}
