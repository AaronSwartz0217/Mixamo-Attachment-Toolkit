using UnityEngine;

namespace Codex.MixamoAttachments
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mixamo Attachments/Attachment Socket")]
    public sealed class AttachmentSocket : MonoBehaviour
    {
        [SerializeField] private AttachmentType socketType;
        [SerializeField] private string socketId;
        [SerializeField] private HumanBodyBones humanBone = HumanBodyBones.LastBone;
        [SerializeField] private string mixamoBoneFallback;
        [SerializeField, HideInInspector] private bool generatedByToolkit;
        [SerializeField, HideInInspector] private int dataVersion = 1;

        public AttachmentType SocketType => socketType;
        public string SocketId => string.IsNullOrWhiteSpace(socketId) ? socketType.ToString() : socketId;
        public HumanBodyBones HumanBone => humanBone;
        public string MixamoBoneFallback => mixamoBoneFallback;
        public bool GeneratedByToolkit => generatedByToolkit;

        public void Configure(
            AttachmentType type,
            string id,
            HumanBodyBones bone,
            string fallbackName,
            bool generated)
        {
            socketType = type;
            socketId = id;
            humanBone = bone;
            mixamoBoneFallback = fallbackName;
            generatedByToolkit = generated;
            dataVersion = 1;
        }
    }
}
