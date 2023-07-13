using System;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 客户端准备就绪
    /// </summary>
    internal struct ReadyEvent : IEvent
    {
    }

    /// <summary>
    /// 客户端取消准备
    /// </summary>
    internal struct NotReadyEvent : IEvent
    {
    }

    /// <summary>
    /// 场景改变
    /// </summary>
    internal struct SceneEvent : IEvent
    {
        public string sceneName;
    }

    /// <summary>
    /// 改变权限
    /// </summary>
    internal struct ChangeOwnerEvent : IEvent
    {
        public uint netId;
        public bool isOwner;
    }

    /// <summary>
    /// 客户端到服务器的Ping
    /// </summary>
    internal struct PingEvent : IEvent
    {
        public readonly double clientTime;
        public PingEvent(double clientTime) => this.clientTime = clientTime;
    }

    /// <summary>
    /// 服务器到客户端的Pong
    /// </summary>
    internal struct PongEvent : IEvent
    {
        public readonly double clientTime;
        public PongEvent(double clientTime) => this.clientTime = clientTime;
    }

    internal struct RpcBufferEvent : IEvent
    {
        public ArraySegment<byte> segment;

        public RpcBufferEvent(ArraySegment<byte> segment) => this.segment = segment;
    }

    internal struct ServerRpcEvent : IEvent
    {
        public uint netId;
        public byte component;
        public ushort funcHash;
        public ArraySegment<byte> segment;
    }

    internal struct ClientRpcEvent : IEvent
    {
        public uint netId;
        public byte component;
        public ushort funcHash;
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