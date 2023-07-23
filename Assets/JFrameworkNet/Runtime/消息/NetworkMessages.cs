using System;
using System.Net;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    public interface Message
    {
    }
        
    /// <summary>
    /// 快照差值
    /// </summary>
    internal struct SnapshotMessage : Message
    {
    }
    
    /// <summary>
    /// 客户端准备就绪
    /// </summary>
    internal struct SetReadyMessage : Message
    {
    }

    /// <summary>
    /// 客户端取消准备
    /// </summary>
    internal struct NotReadyMessage : Message
    {
    }

    /// <summary>
    /// 场景改变
    /// </summary>
    internal struct SceneMessage : Message
    {
        public readonly string sceneName;
        public SceneMessage(string sceneName) => this.sceneName = sceneName;
    }

    /// <summary>
    /// 客户端到服务器的Ping
    /// </summary>
    internal struct PingMessage : Message
    {
        public readonly double clientTime;
        public PingMessage(double clientTime) => this.clientTime = clientTime;
    }

    /// <summary>
    /// 服务器到客户端的Pong
    /// </summary>
    internal struct PongMessage : Message
    {
        public readonly double clientTime;
        public PongMessage(double clientTime) => this.clientTime = clientTime;
    }
    
    /// <summary>
    /// 客户端远程调用服务器
    /// </summary>
    internal struct ServerRpcMessage : Message
    {
        public uint objectId;
        public byte serialId;
        public ushort methodHash;
        public ArraySegment<byte> segment;
    }

    /// <summary>
    /// Rpc缓存事件
    /// </summary>
    internal struct InvokeRpcMessage : Message
    {
        public readonly ArraySegment<byte> segment;
        public InvokeRpcMessage(ArraySegment<byte> segment) => this.segment = segment;
    }

    /// <summary>
    /// 服务器远程调用客户端
    /// </summary>
    internal struct ClientRpcMessage : Message
    {
        public uint objectId;
        public byte serialId;
        public ushort methodHash;
        public ArraySegment<byte> segment;
    }

    /// <summary>
    /// 网络对象生成
    /// </summary>
    internal struct SpawnMessage : Message
    {
        public bool isOwner;
        public uint assetId;
        public uint objectId;
        public ulong sceneId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public ArraySegment<byte> segment;
    }
    
    /// <summary>
    /// 网络对象重置
    /// </summary>
    internal struct DespawnMessage : Message
    {
        public readonly uint objectId;
        public DespawnMessage(uint objectId) => this.objectId = objectId;
    }

    /// <summary>
    /// 网络对象销毁
    /// </summary>
    internal struct DestroyMessage : Message
    {
        public readonly uint objectId;
        public DestroyMessage(uint objectId) => this.objectId = objectId;
    }
    
    /// <summary>
    /// 对象事件
    /// </summary>
    internal struct EntityMessage : Message
    {
        public readonly uint objectId;
        public readonly ArraySegment<byte> segment;

        public EntityMessage(uint objectId, ArraySegment<byte> segment)
        {
            this.objectId = objectId;
            this.segment = segment;
        }
    }
}