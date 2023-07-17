using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    public struct NetworkIdentitySerialization
    {
        public int tick;
        public NetworkWriter owner;
        public NetworkWriter observer;
    }
    
    [DefaultExecutionOrder(-1)]
    public sealed partial class NetworkObject : MonoBehaviour
    {
        private NetworkIdentitySerialization lastSerialization = new NetworkIdentitySerialization
        {
            owner = new NetworkWriter(),
            observer = new NetworkWriter()
        };
        
        private static readonly Dictionary<ulong, NetworkObject> sceneIds = new Dictionary<ulong, NetworkObject>();
        
        [ReadOnly, SerializeField] internal uint assetId;
        [ReadOnly, SerializeField] internal ulong sceneId;
        [ReadOnly, ShowInInspector] internal uint objectId;
        [ReadOnly, ShowInInspector] internal bool isOwner;
        [ReadOnly, ShowInInspector] internal bool isServer;
        [ReadOnly, ShowInInspector] internal bool isClient;
        [ReadOnly, ShowInInspector] internal ServerEntity server;
        [ReadOnly, ShowInInspector] internal ClientEntity client;
        private bool isStartClient;
        private bool hasAuthority;
        internal NetworkBehaviour[] entities;

        private void Awake()
        {
            entities = GetComponentsInChildren<NetworkBehaviour>(true);
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
        
        internal NetworkIdentitySerialization GetServerSerializationAtTick(int tick)
        {
            if (lastSerialization.tick != tick)
            {
                lastSerialization.owner.position = 0;
                lastSerialization.observer.position = 0;

                SerializeServer(false, lastSerialization.owner, lastSerialization.observer);
                
                ClearDirty(true);
                lastSerialization.tick = tick;
            }
            
            return lastSerialization;
        }


        internal void ClearDirty(bool isTotal = false)
        {
            foreach (NetworkBehaviour entity in entities)
            {
                if (entity.IsDirty() || isTotal)
                {
                    entity.ClearDirty();
                }
            }
        }

        /// <summary>
        /// 处理Rpc事件
        /// </summary>
        internal void InvokeRpcEvent(byte index, ushort function, RpcType rpcType, NetworkReader reader, ClientEntity client = null)
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

            NetworkBehaviour invokeComponent = entities[index];
            if (!NetworkRpc.Invoke(function, rpcType, reader, invokeComponent, client))
            {
                Debug.LogError($"无法调用{rpcType} [{function}] 网络对象：{gameObject.name} 网络Id：{objectId}");
            }
        }

        /// <summary>
        /// 在Server端中序列化
        /// </summary>
        /// <param name="serialize"></param>
        /// <param name="owner"></param>
        /// <param name="observer"></param>
        internal void SerializeServer(bool serialize, NetworkWriter owner,NetworkWriter observer)
        {
            IsValid();
            NetworkBehaviour[] components = entities;
            var (ownerMask, observerMask) = ServerDirtyMasks(serialize);
            if (ownerMask != 0) Compression.CompressVarUInt(owner, ownerMask);
            if (observerMask != 0) Compression.CompressVarUInt(observer, observerMask);
            if ((ownerMask | observerMask) != 0)
            {
                for (int i = 0; i < components.Length; ++i)
                {
                    NetworkBehaviour comp = components[i];
                    bool ownerDirty = IsDirty(ownerMask, i);
                    bool observersDirty = IsDirty(observerMask, i);
                    if (ownerDirty || observersDirty)
                    {
                        using var writer = NetworkWriter.Pop();
                        comp.Serialize(writer, serialize);
                        var segment = writer.ToArraySegment();
                        if (ownerDirty) owner.WriteBytes(segment.Array, segment.Offset, segment.Count);
                        if (observersDirty) observer.WriteBytes(segment.Array, segment.Offset, segment.Count);
                    }
                }
            }
        }

        private (ulong, ulong) ServerDirtyMasks(bool initialState)
        {
            ulong ownerMask = 0;
            ulong observerMask = 0;

            NetworkBehaviour[] components = entities;
            for (int i = 0; i < components.Length; ++i)
            {
                NetworkBehaviour component = components[i];

                bool dirty = component.IsDirty();
                ulong nthBit = 1u << i;
                if (initialState || (component.syncDirection == SyncDirection.ServerToClient && dirty))
                {
                    ownerMask |= nthBit;
                }
                if (initialState || dirty)
                {
                    observerMask |= nthBit;
                }
            }

            return (ownerMask, observerMask);
        }
        
        internal void SerializeClient(NetworkWriter writer)
        {
            IsValid();
            NetworkBehaviour[] components = entities;
            ulong dirtyMask = ClientDirtyMask();

            if (dirtyMask != 0)
            {
                Compression.CompressVarUInt(writer, dirtyMask);
                for (int i = 0; i < components.Length; ++i)
                {
                    NetworkBehaviour component = components[i];

                    if (IsDirty(dirtyMask, i))
                    {
                        component.Serialize(writer, false);
                    }
                }
            }
        }
        
        private ulong ClientDirtyMask()
        {
            ulong mask = 0;
            NetworkBehaviour[] components = entities;
            for (int i = 0; i < components.Length; ++i)
            {
                NetworkBehaviour component = components[i];
                if (isOwner && component.syncDirection == SyncDirection.ClientToServer)
                {
                    if (component.IsDirty()) mask |= (1u << i);
                }
            }

            return mask;
        }

        internal bool DeserializeServer(NetworkReader reader)
        {
            IsValid();
            NetworkBehaviour[] components = entities;

            ulong mask = Compression.DecompressVarUInt(reader);

            for (int i = 0; i < components.Length; ++i)
            {
                if (IsDirty(mask, i))
                {
                    NetworkBehaviour component = components[i];

                    if (component.syncDirection == SyncDirection.ClientToServer)
                    {
                        if (!component.Deserialize(reader, false)) return false;

                        component.SetDirty();
                    }
                }
            }

            return true;
        }

        internal void DeserializeClient(NetworkReader reader, bool initialState)
        {
            IsValid();
            NetworkBehaviour[] components = entities;
            
            ulong mask = Compression.DecompressVarUInt(reader);
            
            for (int i = 0; i < components.Length; ++i)
            {
                if (IsDirty(mask, i))
                {
                    NetworkBehaviour comp = components[i];
                    comp.Deserialize(reader, initialState);
                }
            }
        }
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDirty(ulong mask, int index) => (mask & (ulong)(1 << index)) != 0;

        internal void Reset()
        {
            objectId = 0;
            isOwner = false;
            isClient = false;
            isServer = false;
            isStartClient = false;
            hasAuthority = false;
            client = null;
            server = null;
            sceneIds.Clear();
        }
    }
}