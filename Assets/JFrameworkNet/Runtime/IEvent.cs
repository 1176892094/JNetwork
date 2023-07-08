using System;
using UnityEngine;

namespace JFramework.Net
{

    public struct SceneMessage : IEvent
    {
        public string sceneName;
    }

    public struct ReadyMessage : IEvent
    {
    }

    public struct NotReadyMessage : IEvent
    {
    }

    public struct ChangeOwnerMessage : IEvent
    {
        public uint netId;
        public bool isOwner;
    }

    public struct ObjectDestroyMessage : IEvent
    {
        public uint netId;
    }

    public struct ObjectHideMessage : IEvent
    {
        public uint netId;
    }

    public struct ObjectSpawnStartMessage : IEvent
    {
    }

    public struct ObjectSpawnFinishMessage : IEvent
    {
    }

    public struct RpcBufferMessage : IEvent
    {
        public ArraySegment<byte> payload;
    }

    public struct NetworkPingMessage : IEvent
    {
        public readonly double clientTime;

        public NetworkPingMessage(double value)
        {
            clientTime = value;
        }
    }

    public struct CommandMessage : IEvent
    {
        public uint netId;
        public byte componentIndex;
        public ushort functionHash;
        public ArraySegment<byte> payload;
    }

    public struct SpawnMessage : IEvent
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
    
    public struct SnapshotMessage : IEvent
    {
    }
    
    public struct EntityMessage : IEvent
    {
        public uint netId;
        public ArraySegment<byte> segment;
    }
}