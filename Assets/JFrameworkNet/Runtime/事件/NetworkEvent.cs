using System;
using JFramework.Interface;
using UnityEngine;

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

    internal struct RpcBufferEvent : IEvent
    {
        public ArraySegment<byte> payload;
    }

    internal struct PingEvent : IEvent
    {
        public readonly double clientTime;
        public PingEvent(double clientTime) => this.clientTime = clientTime;
    }

    internal struct PongEvent : IEvent
    {
        public readonly double clientTime;
        public PongEvent(double clientTime) => this.clientTime = clientTime;
    }

    internal struct ServerRpcEvent : IEvent
    {
        public uint netId;
        public byte componentIndex;
        public ushort functionHash;
        public ArraySegment<byte> segment;
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
    
    internal struct DestroyEvent : IEvent
    {
        public uint netId;
    }

    internal struct DespawnEvent : IEvent
    {
        public uint netId;
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