using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public abstract partial class NetworkEntity : MonoBehaviour, INetworkEvent
    {
        internal NetworkObject @object;
        internal SyncMode syncMode;
        internal SyncDirection syncDirection;
        public uint netId => @object.netId;
        public bool isOwner => @object.isOwner;
        public bool isServer => @object.isServer;
        public bool isClient => @object.isClient;
        public ServerEntity server => @object.server;
        // 连接的客户端
        public ClientEntity connection => @object.connection;
        internal byte component;


        public byte componentId;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDirty()
        {
            //TODO:
            return false;
        }
    }
}