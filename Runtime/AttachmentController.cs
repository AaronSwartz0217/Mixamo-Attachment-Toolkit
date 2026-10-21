using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codex.MixamoAttachments
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mixamo Attachments/Attachment Controller")]
    public sealed class AttachmentController : MonoBehaviour
    {
        [SerializeField] private bool refreshSocketsOnAwake = true;
        [SerializeField] private AttachmentState currentState = AttachmentState.Default;
        [SerializeField, Min(0f)] private float fallbackTransitionSpeed = 10f;

        private readonly Dictionary<string, AttachmentSocket> sockets =
            new Dictionary<string, AttachmentSocket>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AttachmentItem> equipped =
            new Dictionary<string, AttachmentItem>(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            if (refreshSocketsOnAwake)
                RefreshSockets();
        }

        private void LateUpdate()
        {
            foreach (var pair in equipped)
            {
                var item = pair.Value;
                if (item == null)
                    continue;

                var pose = item.GetPose(currentState);
                var speed = item.Profile != null ? item.Profile.TransitionSpeed : fallbackTransitionSpeed;
                var t = speed <= 0f ? 1f : 1f - Mathf.Exp(-speed * Time.deltaTime);
                var target = item.transform;
                target.localPosition = Vector3.Lerp(target.localPosition, pose.localPosition, t);
                target.localRotation = Quaternion.Slerp(target.localRotation, pose.Rotation, t);
                target.localScale = Vector3.Lerp(target.localScale, pose.localScale, t);
            }
        }

        public void SetState(AttachmentState state, bool immediate = false)
        {
            currentState = state;
            if (!immediate)
                return;

            foreach (var pair in equipped)
            {
                var item = pair.Value;
                if (item != null)
                    item.GetPose(currentState).Apply(item.transform);
            }
        }

        public AttachmentState CurrentState => currentState;

        public void RefreshSockets()
        {
            sockets.Clear();
            var found = GetComponentsInChildren<AttachmentSocket>(true);

            foreach (var socket in found)
            {
                if (socket == null || string.IsNullOrWhiteSpace(socket.SocketId))
                    continue;

                if (!sockets.ContainsKey(socket.SocketId))
                    sockets.Add(socket.SocketId, socket);
            }
        }

        public Transform GetSocket(string socketId)
        {
            if (string.IsNullOrWhiteSpace(socketId))
                return null;

            if (sockets.Count == 0)
                RefreshSockets();

            return sockets.TryGetValue(socketId, out var socket) && socket != null
                ? socket.transform
                : null;
        }

        public GameObject Equip(GameObject itemPrefab)
        {
            if (itemPrefab == null)
                return null;

            var definition = itemPrefab.GetComponent<AttachmentItem>();
            if (definition == null)
            {
                Debug.LogError($"Attachment prefab '{itemPrefab.name}' has no AttachmentItem component.", this);
                return null;
            }

            return Equip(itemPrefab, definition.PreferredSocketId);
        }

        public GameObject Equip(GameObject itemPrefab, AttachmentType type)
        {
            return Equip(itemPrefab, type.ToString());
        }

        public GameObject Equip(GameObject itemPrefab, string socketId)
        {
            if (itemPrefab == null)
                return null;

            var socket = GetSocket(socketId);
            if (socket == null)
            {
                Debug.LogError($"Attachment socket '{socketId}' was not found under '{name}'.", this);
                return null;
            }

            var definition = itemPrefab.GetComponent<AttachmentItem>();
            if (definition == null)
            {
                Debug.LogError($"Attachment prefab '{itemPrefab.name}' has no AttachmentItem component.", this);
                return null;
            }

            Unequip(socketId);

            var instance = Instantiate(itemPrefab, socket, false);
            var item = instance.GetComponent<AttachmentItem>();
            item.GetPose(currentState).Apply(instance.transform);
            equipped[socketId] = item;
            return instance;
        }

        public bool Attach(AttachmentItem item)
        {
            return item != null && Attach(item, item.PreferredSocketId);
        }

        public bool Attach(AttachmentItem item, string socketId)
        {
            if (item == null)
                return false;

            var socket = GetSocket(socketId);
            if (socket == null)
                return false;

            Unequip(socketId);
            item.transform.SetParent(socket, false);
            item.GetPose(currentState).Apply(item.transform);
            equipped[socketId] = item;
            return true;
        }

        public bool Unequip(AttachmentType type)
        {
            return Unequip(type.ToString());
        }

        public bool Unequip(string socketId)
        {
            if (string.IsNullOrWhiteSpace(socketId))
                return false;

            if (!equipped.TryGetValue(socketId, out var item) || item == null)
            {
                equipped.Remove(socketId);
                return false;
            }

            equipped.Remove(socketId);
            Destroy(item.gameObject);
            return true;
        }
    }
}
