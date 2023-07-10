using UnityEngine;

namespace JFramework.Net
{
    public abstract class NetworkEntity : MonoBehaviour
    {
        private NetworkObject @object;
        public uint netId => @object.netId;

        public byte componentId;

        protected void SendRPCInternal(string methodName, int methodHash, NetworkWriter writer, int channel)
        {
            
        }
    }
}