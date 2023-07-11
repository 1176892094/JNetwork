using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    public sealed class NetworkObject : MonoBehaviour
    {
        private readonly Dictionary<int, ClientEntity> observers = new Dictionary<int, ClientEntity>();
        public uint netId;
        public uint assetId;
        public ulong sceneId;
        public bool isOwner;
        public bool isServer;
        public bool isClient;
        private ClientEntity client;

        public ClientEntity connection
        {
            get => client;
            set
            {
                client = value;
                client?.AddToObserver(this);
            }
        }

        public NetworkEntity[] objects;

        internal void AddObserver(ClientEntity client)
        {
            if (observers.ContainsKey(client.clientId)) return;
            observers[client.clientId] = client;
            client.AddToObserver(this);
        }

        /// <summary>
        /// 处理Rpc事件
        /// </summary>
        internal void InvokeRpcEvent(byte index, ushort function, RpcType rpcType, NetworkReader reader, ClientEntity client = null)
        {
            if (this == null)
            {
                Debug.LogWarning($"{rpcType} [{function}] received for deleted object netId: {netId}");
                return;
            }

            if (index >= objects.Length)
            {
                Debug.LogWarning($"Component [{index}] not found for netId: {netId}");
                return;
            }

            NetworkEntity invokeComponent = objects[index];
            if (!RpcUtils.Invoke(function, rpcType, reader, invokeComponent, client))
            {
                Debug.LogError($"No found received for {rpcType} [{function}] on {gameObject.name} netId = {netId}");
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
            if (observers == null)
            {
                Debug.LogError($"NetworkEntity component is empty", gameObject);
                return;
            }

            if (observers.Count > NetworkConst.MaxEntityCount)
            {
                Debug.LogError($"The number of NetworkEntity cannot be greater than {NetworkConst.MaxEntityCount}");
            }

            NetworkEntity[] entities = objects;

            (ulong ownerMask, ulong observerMask) = ServerDirtyMasks(isInit);
        }

        private (ulong, ulong) ServerDirtyMasks(bool isInit)
        {
            ulong ownerMask = 0;
            ulong observerMask = 0;

            NetworkEntity[] components = objects;
            for (int i = 0; i < components.Length; ++i)
            {
                NetworkEntity component = components[i];

                bool dirty = component.IsDirty();
                ulong nthBit = 1U << i;

                if (isInit || (component.syncDirection == SyncDirection.ServerToClient && dirty))
                {
                    ownerMask |= nthBit;
                }

                if (component.syncMode == SyncMode.Observer && (isInit || dirty))
                {
                    observerMask |= nthBit;
                }
            }

            return (ownerMask, observerMask);
        }
    }
}