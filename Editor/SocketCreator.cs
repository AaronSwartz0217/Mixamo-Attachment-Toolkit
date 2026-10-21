using UnityEditor;
using UnityEngine;

namespace Codex.MixamoAttachments.Editor
{
    internal static class SocketCreator
    {
        public static AttachmentSocket FindExisting(GameObject characterRoot, SocketDefinition definition)
        {
            if (characterRoot == null)
                return null;

            var sockets = characterRoot.GetComponentsInChildren<AttachmentSocket>(true);
            foreach (var socket in sockets)
            {
                if (socket.SocketType == definition.Type || socket.SocketId == definition.SocketId)
                    return socket;
            }

            return null;
        }

        public static AttachmentSocket CreateOrRepair(
            GameObject characterRoot,
            Animator animator,
            SocketDefinition definition)
        {
            var bone = MixamoBoneResolver.Resolve(characterRoot, animator, definition);
            if (bone == null)
                return null;

            var existing = FindExisting(characterRoot, definition);
            if (existing != null)
                return existing;

            var socketObject = new GameObject(definition.SocketName);
            Undo.RegisterCreatedObjectUndo(socketObject, $"创建 {definition.DisplayName} Socket");
            socketObject.transform.SetParent(bone, false);
            socketObject.transform.localPosition = Vector3.zero;
            socketObject.transform.localRotation = Quaternion.identity;
            socketObject.transform.localScale = Vector3.one;

            var socket = Undo.AddComponent<AttachmentSocket>(socketObject);
            socket.Configure(
                definition.Type,
                definition.SocketId,
                definition.HumanBone,
                definition.MixamoBoneName,
                true);

            EditorUtility.SetDirty(socket);
            return socket;
        }
    }
}
