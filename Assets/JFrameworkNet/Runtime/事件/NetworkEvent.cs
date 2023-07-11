using System;
using JFramework.Interface;
using UnityEngine;
using UnityEngine.Serialization;

namespace JFramework.Net
{
    internal struct SceneEvent : IEvent
    {
        public string sceneName;
    }

    internal struct ReadyEvent : IEvent
    {
    }

    internal struct NotReadyEvent : IEvent
    {
    }

    internal struct ChangeOwnerEvent : IEvent
    {
        public uint netId;
        public bool isOwner;
    }

    internal struct ObjectDestroyEvent : IEvent
    {
        public uint netId;
    }

    internal struct ObjectDespawnEvent : IEvent
    {
        public uint netId;
    }

    internal struct ObjectSpawnStartEvent : IEvent
    {
    }

    internal struct ObjectSpawnFinishEvent : IEvent
    {
    }

    internal struct RpcBufferEvent : IEvent
    {
        public ArraySegment<byte> payload;
    }

    internal struct PingEvent : IEvent
    {
        public double clientTime;
    }

    internal struct PongEvent : IEvent
    {
        public double clientTime;
    }

    internal struct CommandEvent : IEvent
    {
        public uint netId;
        public byte componentIndex;
        public ushort functionHash;
        public ArraySegment<byte> payload;
    }

    internal struct SpawnEvent : IEvent
    {
        public uint netId;
        public bool isOwner;
        public ulong sceneId;
        public uint assetId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public ArraySegment<byte> segment;
    }

    internal struct SnapshotEvent : IEvent
    {
    }

    internal struct EntityEvent : IEvent
    {
        public uint netId;
        public ArraySegment<byte> segment;
    }
}