using System;
using UnityEngine;

namespace JFramework.Net
{
    public sealed class NetworkObject : MonoBehaviour
    {
        public uint netId;
        public ulong sceneId;
        public bool isOwner;
        public bool isServer;
        public bool isClient;
        private bool isStartClient;
        private bool hasAuthority;
 
        private uint m_assetId;

        internal uint assetId
        {
            get => m_assetId;
            set
            {
                if (value == 0)
                {
                    Debug.LogError("The assetId of a NetworkObject cannot be zero");
                    return;
                }
                m_assetId = value;
            }
        }

        internal ClientEntity m_connection;
        public ClientEntity connection
        {
            get => m_connection;
            private set => m_connection = value;
        }

        public NetworkEntity[] objects;

        private void Awake()
        {
            objects = GetComponentsInChildren<NetworkEntity>(true);
            
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
            if (objects == null)
            {
                Debug.LogError($"NetworkEntity component is empty", gameObject);
                return;
            }

            if (objects.Length > NetworkConst.MaxEntityCount)
            {
                Debug.LogError($"The number of NetworkEntity cannot be greater than {NetworkConst.MaxEntityCount}");
                return;
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
        
        /// <summary>
        /// 仅在客户端调用，触发Notify则进行权限认证
        /// </summary>
        internal void OnNotifyAuthority()
        {
            if (!hasAuthority && isOwner)
            {
                OnStartAuthority();
            }
            else if (hasAuthority && !isOwner)
            {
                OnStopAuthority();
            }

            hasAuthority = isOwner;
        }
        
        /// <summary>
        /// 仅在客户端调用，当在客户端生成时调用
        /// </summary>
        internal void OnStartClient()
        {
            if (isStartClient) return;
            isStartClient = true;
            
            foreach (var entity in objects)
            {
                try
                {
                    entity.GetComponent<IStartClient>()?.OnStartClient();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }
        
        /// <summary>
        /// 仅在客户端调用，当在客户端销毁时调用
        /// </summary>
        internal void OnStopClient()
        {
            if (!isStartClient) return;

            foreach (var entity in objects)
            {
                try
                {
                    entity.GetComponent<IStopClient>()?.OnStopClient();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }

        /// <summary>
        /// 仅在服务器上调用，当在服务器生成时调用
        /// </summary>
        internal void OnStartServer()
        {
            foreach (var entity in objects)
            {
                try
                {
                    entity.GetComponent<IStartServer>()?.OnStartServer();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }

        /// <summary>
        /// 仅在客户端调用，当通过验证时调用
        /// </summary>
        private void OnStartAuthority()
        {
            foreach (var entity in objects)
            {
                try
                {
                    entity.GetComponent<IStartAuthority>()?.OnStartAuthority();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }
        
        /// <summary>
        /// 仅在客户端调用，当停止验证时调用
        /// </summary>
        private void OnStopAuthority()
        {
            foreach (var entity in objects)
            {
                try
                {
                    entity.GetComponent<IStopAuthority>()?.OnStopAuthority();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity);
                }
            }
        }

        internal void Reset()
        {
            netId = 0;
            isOwner = false;
            isClient = false;
            isServer = false;
            isStartClient = false;
            hasAuthority = false;
            connection = null;
        }
    }
}