using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkObject : MonoBehaviour
    {
        private static readonly Dictionary<ulong, NetworkObject> sceneIds = new Dictionary<ulong, NetworkObject>();
        
        [ReadOnly, ShowInInspector] internal uint assetId;
        [ReadOnly, ShowInInspector] internal uint objectId;
        [ReadOnly, ShowInInspector] internal ulong sceneId;
        [ReadOnly, ShowInInspector] public bool isOwner;
        [ReadOnly, ShowInInspector] public bool isServer;
        [ReadOnly, ShowInInspector] public bool isClient;
        [ReadOnly, ShowInInspector] internal NetworkServerEntity server;
        [ReadOnly, ShowInInspector] internal NetworkClientEntity client;
        private bool isStartClient;
        private bool hasAuthority;
        internal NetworkEntity[] entities;

        private void Awake()
        {
            entities = GetComponentsInChildren<NetworkEntity>(true);
            if (IsValid())
            {
                for (int i = 0; i < entities.Length; ++i)
                {
                    entities[i].@object = this;
                    entities[i].serialId = (byte)i;
                }
            }
        }

        private bool IsValid()
        {
            if (entities == null)
            {
                Debug.LogError($"网络对象持有的 NetworkEntity 为空", gameObject);
                return false;
            }

            if (entities.Length > NetworkConst.MaxEntityCount)
            {
                Debug.LogError($"网络对象持有的 NetworkEntity 的数量不能超过{NetworkConst.MaxEntityCount}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 处理Rpc事件
        /// </summary>
        internal void InvokeRpcEvent(byte index, ushort function, RpcType rpcType, NetworkReader reader, NetworkClientEntity client = null)
        {
            if (this == null)
            {
                Debug.LogWarning($"调用了已经删除的网络对象。{rpcType} [{function}] 网络Id：{objectId}");
                return;
            }

            if (index >= entities.Length)
            {
                Debug.LogWarning($"没有找到组件Id：[{index}] 网络Id：{objectId}");
                return;
            }

            NetworkEntity invokeComponent = entities[index];
            if (!RpcUtils.Invoke(function, rpcType, reader, invokeComponent, client))
            {
                Debug.LogError($"无法调用{rpcType} [{function}] 网络对象：{gameObject.name} 网络Id：{objectId}");
            }
        }

        /// <summary>
        /// 在Server端中序列化
        /// </summary>
        /// <param name="isInit"></param>
        /// <param name="observer"></param>
        internal void SerializeServer(bool isInit,  NetworkWriter observer)
        {
            if (IsValid())
            {
                NetworkEntity[] entities = this.entities;

                (ulong ownerMask, ulong observerMask) = ServerDirtyMasks(isInit);
            }
        }

        private (ulong, ulong) ServerDirtyMasks(bool isInit)
        {
            ulong ownerMask = 0;
            ulong observerMask = 0;

            NetworkEntity[] components = entities;
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

        internal void Reset()
        {
            objectId = 0;
            isOwner = false;
            isClient = false;
            isServer = false;
            isStartClient = false;
            hasAuthority = false;
            client = null;
        }
    }
}