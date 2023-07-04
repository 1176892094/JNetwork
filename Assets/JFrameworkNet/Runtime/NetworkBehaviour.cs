using UnityEngine;

namespace JFramework.Net
{
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        private NetworkIdentity identity;
        public uint netId => identity.netId;

        public byte componentId;
    }
}