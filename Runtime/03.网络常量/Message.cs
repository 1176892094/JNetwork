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
using UnityEngine;

namespace JFramework.Net
{
    public interface Message
    {
    }

    internal struct ReadyMessage : Message
    {
        public readonly bool ready;

        public ReadyMessage(bool ready)
        {
            this.ready = ready;
        }
    }

    internal struct SceneMessage : Message
    {
        public readonly string sceneName;
        public SceneMessage(string sceneName) => this.sceneName = sceneName;
    }

    internal struct PingMessage : Message
    {
        public readonly double clientTime;
        public PingMessage(double clientTime) => this.clientTime = clientTime;
    }

    internal struct ServerRpcMessage : Message
    {
        public uint objectId;
        public byte componentId;
        public ushort methodHash;
        public ArraySegment<byte> segment;
    }

    internal struct ClientRpcMessage : Message
    {
        public uint objectId;
        public byte componentId;
        public ushort methodHash;
        public ArraySegment<byte> segment;
    }

    internal struct SpawnMessage : Message
    {
        public bool isOwner;
        public bool usePool;
        public uint objectId;
        public ulong sceneId;
        public string assetPath;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public ArraySegment<byte> segment;
    }

    internal struct DespawnMessage : Message
    {
        public readonly uint objectId;
        public DespawnMessage(uint objectId) => this.objectId = objectId;
    }

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

    internal struct RequestMessage : Message
    {
    }

    internal struct ResponseMessage : Message
    {
        public Uri uri;
        public ResponseMessage(Uri uri) => this.uri = uri;
    }
}