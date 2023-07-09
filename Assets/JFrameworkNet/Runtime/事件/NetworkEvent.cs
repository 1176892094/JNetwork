using System;
using UnityEngine;

namespace JFramework.Net
{
    public struct SceneEvent : IEvent
    {
        public string sceneName;
    }

    public struct ReadyEvent : IEvent
    {
    }

    public struct NotReadyEvent : IEvent
    {
    }

    public struct ChangeOwnerEvent : IEvent
    {
        public uint netId;
        public bool isOwner;
    }

    public struct ObjectDestroyEvent : IEvent
    {
        public uint netId;
    }

    public struct ObjectHideEvent : IEvent
    {
        public uint netId;
    }

    public struct ObjectSpawnStartEvent : IEvent
    {
    }

    public struct ObjectSpawnFinishEvent : IEvent
    {
    }

    public struct RpcBufferEvent : IEvent
    {
        public ArraySegment<byte> payload;
    }

    public struct PingEvent : IEvent
    {
        public double clientTime;
    }
    
    public struct PongEvent : IEvent
    {
        public double clientTime;
    }

    public struct CommandEvent : IEvent
    {
        public uint netId;
        public byte componentIndex;
        public ushort functionHash;
        public ArraySegment<byte> payload;
    }

    public struct SpawnEvent : IEvent
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
    
    public struct SnapshotEvent : IEvent
    {
    }
    
    public struct EntityEvent : IEvent
    {
        public uint netId;
        public ArraySegment<byte> segment;
    }
}