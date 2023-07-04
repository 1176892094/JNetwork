using System;
using UnityEngine;

namespace JFramework.Net
{
    public interface NetworkMessage
    {
    }

    public struct SceneMessage : NetworkMessage
    {
        public string sceneName;
    }

    public struct ReadyMessage : NetworkMessage
    {
    }

    public struct NotReadyMessage : NetworkMessage
    {
    }

    public struct ChangeOwnerMessage : NetworkMessage
    {
        public uint netId;
        public bool isOwner;
    }

    public struct ObjectDestroyMessage : NetworkMessage
    {
        public uint netId;
    }

    public struct ObjectHideMessage : NetworkMessage
    {
        public uint netId;
    }

    public struct ObjectSpawnStartMessage : NetworkMessage
    {
    }

    public struct ObjectSpawnFinishMessage : NetworkMessage
    {
    }

    public struct RpcBufferMessage : NetworkMessage
    {
        public ArraySegment<byte> payload;
    }

    public struct NetworkPingMessage : NetworkMessage
    {
        public readonly double clientTime;

        public NetworkPingMessage(double value)
        {
            clientTime = value;
        }
    }

    public struct CommandMessage : NetworkMessage
    {
        public uint netId;
        public byte componentIndex;
        public ushort functionHash;
        public ArraySegment<byte> payload;
    }

    public struct SpawnMessage : NetworkMessage
    {
        public uint netId;
        public bool isOwner;
        public ulong sceneId;
        public uint assetId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public ArraySegment<byte> payload;
    }
}