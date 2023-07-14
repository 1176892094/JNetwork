using System;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 快照差值
    /// </summary>
    internal struct SnapshotEvent : IEvent
    {
    }
    
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
        public readonly string sceneName;
        public SceneEvent(string sceneName) => this.sceneName = sceneName;
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

    /// <summary>
    /// Rpc缓存事件
    /// </summary>
    internal struct RpcInvokeEvent : IEvent
    {
        public readonly ArraySegment<byte> segment;
        public RpcInvokeEvent(ArraySegment<byte> segment) => this.segment = segment;
    }

    /// <summary>
    /// 客户端远程调用服务器
    /// </summary>
    internal struct ServerRpcEvent : IEvent
    {
        public uint netId;
        public byte component;
        public ushort funcHash;
        public ArraySegment<byte> segment;
    }

    /// <summary>
    /// 服务器远程调用客户端
    /// </summary>
    internal struct ClientRpcEvent : IEvent
    {
        public uint netId;
        public byte component;
        public ushort funcHash;
        public ArraySegment<byte> segment;
    }

    /// <summary>
    /// 网络对象生成
    /// </summary>
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
    
    /// <summary>
    /// 网络对象重置
    /// </summary>
    internal struct DespawnEvent : IEvent
    {
        public readonly uint netId;
        public DespawnEvent(uint netId) => this.netId = netId;
    }

    /// <summary>
    /// 网络对象销毁
    /// </summary>
    internal struct DestroyEvent : IEvent
    {
        public readonly uint netId;
        public DestroyEvent(uint netId) => this.netId = netId;
    }
}