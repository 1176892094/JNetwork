using UnityEngine;

namespace JFramework.Net
{
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        private NetworkObject @object;
        public uint netId => @object.netId;

        public byte componentId;
    }
}