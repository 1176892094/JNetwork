using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    public sealed class NetworkObject : MonoBehaviour
    {
        public readonly Dictionary<int, ClientEntity> observers = new Dictionary<int, ClientEntity>();
        public uint netId;
        public uint assetId;
        public ulong sceneId;
        public ClientEntity connection;
        public NetworkEntity[] objects;

        internal void AddObserver(ClientEntity client)
        {
            if (observers.ContainsKey(client.clientId))
            {
                return;
            }

            observers[client.clientId] = client;
            client.AddToObserver(this);
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

        /// <summary>
        /// 在Server端中序列化
        /// </summary>
        /// <param name="isInit"></param>
        /// <param name="owner"></param>
        /// <param name="observer"></param>
        internal void SerializeServer(bool isInit, NetworkWriter owner, NetworkWriter observer)
        {
        }

        internal void IsValidComponent()
        {
            if (objects == null)
            {
                Debug.LogError("");
            }
        }
    }
}