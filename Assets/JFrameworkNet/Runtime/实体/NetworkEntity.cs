using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public abstract class NetworkEntity : MonoBehaviour
    {
        private NetworkObject @object;
        internal SyncDirection syncDirection;
        internal SyncMode syncMode;
        public uint netId => @object.netId;
        public bool isOwner => @object.isOwner;
        public bool isServer => @object.isServer;
        public bool isClient => @object.isClient;
     

        public byte componentId;

        protected void SendRPCInternal(string methodName, int methodHash, NetworkWriter writer, int channel)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDirty()
        {
            //TODO:
            return false;
        }
    }
}