// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  02:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    internal struct ReadyMessage : IMessage
    {
       
    }
    
    internal struct NotReadyMessage : IMessage
    {
       
    }

    internal struct SceneMessage : IMessage
    {
        public readonly string sceneName;
        public SceneMessage(string sceneName) => this.sceneName = sceneName;
    }

    internal struct PongMessage : IMessage
    {
        public readonly double clientTime;
        public PongMessage(double clientTime) => this.clientTime = clientTime;
    }
    
    internal struct PingMessage : IMessage
    {
        public readonly double clientTime;
        public PingMessage(double clientTime) => this.clientTime = clientTime;
    }

    internal struct ServerRpcMessage : IMessage
    {
        public uint objectId;
        public byte componentId;
        public ushort methodHash;
        public ArraySegment<byte> segment;
    }

    internal struct ClientRpcMessage : IMessage
    {
        public uint objectId;
        public byte componentId;
        public ushort methodHash;
        public ArraySegment<byte> segment;
    }

    internal struct SpawnMessage : IMessage
    {
        public bool isPool;
        public bool isOwner;
        public uint objectId;
        public ulong sceneId;
        public string assetId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public ArraySegment<byte> segment;
    }

    internal struct DespawnMessage : IMessage
    {
        public readonly uint objectId;
        public DespawnMessage(uint objectId) => this.objectId = objectId;
    }

    internal struct EntityMessage : IMessage
    {
        public readonly uint objectId;
        public readonly ArraySegment<byte> segment;

        public EntityMessage(uint objectId, ArraySegment<byte> segment)
        {
            this.objectId = objectId;
            this.segment = segment;
        }
    }

    internal struct RequestMessage : IMessage
    {
    }

    internal struct ResponseMessage : IMessage
    {
        public Uri uri;
        public ResponseMessage(Uri uri) => this.uri = uri;
    }
}