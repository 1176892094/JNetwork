using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    [DefaultExecutionOrder(-1)]
    public sealed partial class NetworkObject : MonoBehaviour
    {
        /// <summary>
        /// 网络变量序列化
        /// </summary>
        internal struct NetworkSerialize
        {
            public int tick;
            public NetworkWriter owner;
            public NetworkWriter observer;
        }
        
        /// <summary>
        /// 上一次序列化间隔
        /// </summary>
        private NetworkSerialize lastSerialize = new NetworkSerialize
        {
            owner = new NetworkWriter(),
            observer = new NetworkWriter()
        };
        
        /// <summary>
        /// 场景Id列表
        /// </summary>
        private static readonly Dictionary<ulong, NetworkObject> sceneIds = new Dictionary<ulong, NetworkObject>();
        
        /// <summary>
        /// 作为资源的路径
        /// </summary>
        [ReadOnly, SerializeField] internal string assetId;
        
        /// <summary>
        /// 作为场景资源的Id
        /// </summary>
        [ReadOnly, SerializeField] internal ulong sceneId;
        
        /// <summary>
        /// 游戏对象Id，用于网络标识
        /// </summary>
        [ReadOnly, ShowInInspector] internal uint objectId;
        
        /// <summary>
        /// 是否有用权限
        /// </summary>
        [ReadOnly, ShowInInspector] internal bool isOwner;
        
        /// <summary>
        /// 是否在服务器端
        /// </summary>
        [ReadOnly, ShowInInspector] internal bool isServer;
        
        /// <summary>
        /// 是否在客户端
        /// </summary>
        [ReadOnly, ShowInInspector] internal bool isClient;
        
        /// <summary>
        /// 连接的客户端 (客户端不可用)
        /// </summary>
        internal NetworkClient connection;
        
        /// <summary>
        /// 是否为第一次生成
        /// </summary>
        private bool isSpawn;

        /// <summary>
        /// NetworkManager.Server.Destroy
        /// </summary>
        internal bool isDestroy;
        
        /// <summary>
        /// 是否经过权限验证
        /// </summary>
        private bool isAuthority;
        
        /// <summary>
        /// 所持有的 NetworkBehaviour
        /// </summary>
        internal NetworkBehaviour[] entities;

        /// <summary>
        /// 初始化获取 NetworkBehaviour
        /// </summary>
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

        /// <summary>
        /// 判断NetworkBehaviour是否有效
        /// </summary>
        /// <returns></returns>
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
        /// 设置为改变
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDirty(ulong mask, int index) => (mask & (ulong)(1 << index)) != 0;
        
        /// <summary>
        /// 服务器帧序列化
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        internal NetworkSerialize ServerSerializeTick(int tick)
        {
            if (lastSerialize.tick != tick)
            {
                lastSerialize.owner.position = 0;
                lastSerialize.observer.position = 0;

                ServerSerialize(false, lastSerialize.owner, lastSerialize.observer);
                
                ClearDirty(true);
                lastSerialize.tick = tick;
            }
            
            return lastSerialize;
        }
        
        /// <summary>
        /// 清除改变值
        /// </summary>
        /// <param name="isTotal"></param>
        internal void ClearDirty(bool isTotal = false)
        {
            foreach (var entity in entities)
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
        internal void InvokeRpcMessage(byte index, ushort function, RpcType rpcType, NetworkReader reader, NetworkClient client = null)
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

            NetworkBehaviour component = entities[index];
            if (!NetworkRpc.Invoke(function, rpcType, reader, component, client))
            {
                Debug.LogError($"无法调用{rpcType} [{function}] 网络对象：{gameObject.name} 网络Id：{objectId}");
            }
        }

        /// <summary>
        /// 服务器序列化 SyncVar
        /// </summary>
        /// <param name="start"></param>
        /// <param name="owner"></param>
        /// <param name="observer"></param>
        internal void ServerSerialize(bool start, NetworkWriter owner, NetworkWriter observer)
        {
            IsValid();
            var components = entities;
            var (ownerMask, observerMask) = ServerDirtyMasks(start);
            if (ownerMask != 0) Compression.CompressVarUInt(owner, ownerMask);
            if (observerMask != 0) Compression.CompressVarUInt(observer, observerMask);
            if ((ownerMask | observerMask) != 0)
            {
                for (int i = 0; i < components.Length; ++i)
                {
                    var component = components[i];
                    bool ownerDirty = IsDirty(ownerMask, i);
                    bool observersDirty = IsDirty(observerMask, i);
                    if (ownerDirty || observersDirty)
                    {
                        using var writer = NetworkWriter.Pop();
                        component.Serialize(writer, start);
                        var segment = writer.ToArraySegment();
                        if (ownerDirty) owner.WriteBytesInternal(segment.Array, segment.Offset, segment.Count);
                        if (observersDirty) observer.WriteBytesInternal(segment.Array, segment.Offset, segment.Count);
                    }
                }
            }
        }

        /// <summary>
        /// 服务器改变遮罩
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        private (ulong, ulong) ServerDirtyMasks(bool start)
        {
            ulong ownerMask = 0;
            ulong observerMask = 0;

            var components = entities;
            for (int i = 0; i < components.Length; ++i)
            {
                var component = components[i];

                var dirty = component.IsDirty();
                ulong nthBit = 1u << i;
                if (start || (component.syncDirection == SyncMode.ServerToClient && dirty))
                {
                    ownerMask |= nthBit;
                }
                if (start || dirty)
                {
                    observerMask |= nthBit;
                }
            }
            
            return (ownerMask, observerMask);
        }
        
        /// <summary>
        /// 客户端序列化 SyncVar
        /// </summary>
        /// <param name="writer"></param>
        internal void ClientSerialize(NetworkWriter writer)
        {
            IsValid();
            var components = entities;
            var dirtyMask = ClientDirtyMask();

            if (dirtyMask != 0)
            {
                Compression.CompressVarUInt(writer, dirtyMask);
                for (int i = 0; i < components.Length; ++i)
                {
                    var component = components[i];

                    if (IsDirty(dirtyMask, i))
                    {
                        component.Serialize(writer, false);
                    }
                }
            }
        }
        
        /// <summary>
        /// 客户端改变遮罩
        /// </summary>
        /// <returns></returns>
        private ulong ClientDirtyMask()
        {
            ulong mask = 0;
            var components = entities;
            for (int i = 0; i < components.Length; ++i)
            {
                var component = components[i];
                if (isOwner && component.syncDirection == SyncMode.ClientToServer)
                {
                    if (component.IsDirty()) mask |= (1u << i);
                }
            }

            return mask;
        }

        /// <summary>
        /// 服务器反序列化 SyncVar
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal bool ServerDeserialize(NetworkReader reader)
        {
            IsValid();
            var components = entities;

            ulong mask = Compression.DecompressVarUInt(reader);

            for (int i = 0; i < components.Length; ++i)
            {
                if (IsDirty(mask, i))
                {
                    var component = components[i];

                    if (component.syncDirection == SyncMode.ClientToServer)
                    {
                        if (!component.Deserialize(reader, false)) return false;

                        component.SetDirty();
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 客户端反序列化 SyncVar
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="start"></param>
        internal void ClientDeserialize(NetworkReader reader, bool start)
        {
            IsValid();
            var components = entities;
            
            var mask = Compression.DecompressVarUInt(reader);
            
            for (int i = 0; i < components.Length; ++i)
            {
                if (IsDirty(mask, i))
                {
                    var component = components[i];
                    component.Deserialize(reader, start);
                }
            }
        }
        
        /// <summary>
        /// 重置NetworkObject
        /// </summary>
        internal void Reset()
        {
            objectId = 0;
            isOwner = false;
            isClient = false;
            isServer = false;
            isSpawn = false;
            isAuthority = false;
            connection = null;
            sceneIds.Clear();
        }

        private void OnDestroy()
        {
            if (isServer && !isDestroy)
            {
                NetworkManager.Server.Destroy(this);
            }

            if (isClient)
            {
                NetworkManager.Client.spawns.Remove(objectId);
            }
        }
    }
}