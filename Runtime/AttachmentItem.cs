using UnityEngine;

namespace Codex.MixamoAttachments
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mixamo Attachments/Attachment Item")]
    public sealed class AttachmentItem : MonoBehaviour
    {
        [SerializeField] private AttachmentType attachmentType;
        [SerializeField] private string preferredSocketId;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localEulerAngles;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [SerializeField] private WeaponAttachmentProfile profile;
        [SerializeField] private string itemId;
        [SerializeField] private Transform optionalGripLeft;
        [SerializeField] private Transform optionalGripRight;
        [SerializeField, HideInInspector] private int dataVersion = 1;

        public AttachmentType Type => attachmentType;
        public string PreferredSocketId => string.IsNullOrWhiteSpace(preferredSocketId)
            ? attachmentType.ToString()
            : preferredSocketId;
        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalEulerAngles => localEulerAngles;
        public Vector3 LocalScale => localScale;
        public WeaponAttachmentProfile Profile => profile;
        public string ItemId => itemId;
        public Transform OptionalGripLeft => optionalGripLeft;
        public Transform OptionalGripRight => optionalGripRight;

        public void Configure(AttachmentType type, string socketId)
        {
            attachmentType = type;
            preferredSocketId = socketId;

            if (string.IsNullOrWhiteSpace(itemId))
                itemId = gameObject.name;

            dataVersion = 1;
        }

        public void SetProfile(WeaponAttachmentProfile value)
        {
            profile = value;
        }

        public AttachmentPose GetPose(AttachmentState state)
        {
            if (profile != null)
                return profile.Evaluate(state);

            return new AttachmentPose
            {
                localPosition = localPosition,
                localEulerAngles = localEulerAngles,
                localScale = localScale
            };
        }

        public void CaptureLocalPose(Transform source)
        {
            if (source == null)
                return;

            localPosition = source.localPosition;
            localEulerAngles = NormalizeEuler(source.localEulerAngles);
            localScale = source.localScale;
        }

        public void ApplyLocalPose(Transform target)
        {
            if (target == null)
                return;

            GetPose(AttachmentState.Default).Apply(target);
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
