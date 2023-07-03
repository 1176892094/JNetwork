using UnityEngine;

namespace JFramework.Net
{
    public class NetworkIdentity : MonoBehaviour
    {
        public uint netId;
        public int sceneId;
        public NetworkBehaviour[] objects;
    }
}