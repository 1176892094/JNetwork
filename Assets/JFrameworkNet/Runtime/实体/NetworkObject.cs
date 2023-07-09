using UnityEngine;

namespace JFramework.Net
{
    public sealed class NetworkObject : MonoBehaviour
    {
        public uint netId;
        public int sceneId;
        public int tickFrame;
        public NetworkWriter writer;
        public ClientEntity client;
        public NetworkEntity[] objects;

        public void AddObserver(ClientEntity client)
        {
        }

        /// <summary>
        /// 处理Rpc事件
        /// </summary>
        internal void HandleRpcEvent(byte componentIndex, ushort functionHash, RpcType rpcType, NetworkReader reader, ClientEntity client = null)
        {
            if (this == null)
            {
                Debug.LogWarning($"{rpcType} [{functionHash}] received for deleted object netId: {netId}");
                return;
            }

            if (componentIndex >= objects.Length)
            {
                Debug.LogWarning($"Component [{componentIndex}] not found for netId: {netId}");
                return;
            }

            NetworkEntity invokeComponent = objects[componentIndex];
            if (!RpcUtils.Invoke(functionHash, rpcType, reader, invokeComponent, client))
            {
                Debug.LogError($"Not found received for {rpcType} [{functionHash}] on {gameObject.name} netId = {netId}");
            }
        }
        
        internal NetworkWriter GetServerSerializationAtTick(int tick)
        {
            if (tickFrame != tick)
            {
                writer.position = 0;
            }
            
            return writer;
        }

        public void OnStopClient()
        {
            
        }
    }
}