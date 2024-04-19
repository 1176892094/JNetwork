using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JFramework.Interface;
using UnityEngine;

// ReSharper disable All

namespace JFramework.Net
{
    /// <summary>
    /// 网络行为对象
    /// </summary>
    public abstract partial class NetworkBehaviour : MonoBehaviour, NetworkEvent
    {
        /// <summary>
        /// 服务器变量的改变选项
        /// </summary>
        protected ulong syncVarDirty { get; set; }

        /// <summary>
        /// 服务器变量的钩子
        /// </summary>
        private ulong syncVarHook;

        /// <summary>
        /// 当前实体在网络对象中的位置
        /// </summary>
        internal byte serialId;

        /// <summary>
        /// 上一次同步时间
        /// </summary>
        private double lastSyncTime;

        /// <summary>
        /// 同步模式
        /// </summary>
        [SerializeField] internal SyncMode syncDirection;

        /// <summary>
        /// 同步间隔
        /// </summary>
        [SerializeField, Range(0, 2)] internal float syncInterval;

        /// <summary>
        /// 网络对象组件
        /// </summary>
        public NetworkObject @object { get; internal set; }

        /// <summary>
        /// 网络对象Id
        /// </summary>
        public uint objectId => @object.objectId;

        /// <summary>
        /// 网络对象权限
        /// </summary>
        public bool isOwner => @object.isOwner;

        /// <summary>
        /// 当前网络对象是否在服务器
        /// </summary>
        public bool isServer => @object.isServer;

        /// <summary>
        /// 当前网络对象是否在客户端
        /// </summary>
        public bool isClient => @object.isClient;

        /// <summary>
        /// 网络对象连接的客户端(服务器不为空，客户端为空)
        /// </summary>
        public NetworkClient connection => @object.connection;

        /// <summary>
        /// 是否能够改变网络值
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDirty()
        {
            return syncVarDirty != 0UL && NetworkManager.Time.localTime - lastSyncTime >= syncInterval;
        }

        /// <summary>
        /// 设置网络变量值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDirty()
        {
            SetSyncVarDirty(ulong.MaxValue);
        }

        public void ClearDirty()
        {
            lastSyncTime = NetworkManager.Time.localTime;
            syncVarDirty = 0L;
        }

        /// <summary>
        /// 设置服务器变量改变
        /// </summary>
        /// <param name="dirty"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetSyncVarDirty(ulong dirty) => syncVarDirty |= dirty;

        /// <summary>
        /// 获取服务器变量的钩子
        /// </summary>
        /// <param name="dirty"></param>
        /// <returns></returns>
        private bool GetSyncVarHook(ulong dirty) => (syncVarHook & dirty) != 0UL;

        /// <summary>
        /// 设置服务器变量的钩子
        /// </summary>
        /// <param name="dirty"></param>
        /// <param name="value"></param>
        private void SetSyncVarHook(ulong dirty, bool value)
        {
            syncVarHook = value ? syncVarHook | dirty : syncVarHook & ~dirty;
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="serialize"></param>
        internal void Serialize(NetworkWriter writer, bool serialize)
        {
            int headerPosition = writer.position;
            writer.WriteByte(0);
            int contentPosition = writer.position;

            try
            {
                OnSerialize(writer, serialize);
            }
            catch (Exception e)
            {
                Debug.LogError($"序列化对象失败。对象名称：{name} 组件：{GetType()} 场景Id：{@object.sceneId:X}\n{e}");
            }

            int endPosition = writer.position;
            writer.position = headerPosition;
            int size = endPosition - contentPosition;
            byte safety = (byte)(size & 0xFF);
            writer.WriteByte(safety);
            writer.position = endPosition;
        }

        /// <summary>
        /// 可以重写这个方法
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="start"></param>
        protected virtual void OnSerialize(NetworkWriter writer, bool start) => SerializeSyncVars(writer, start);

        /// <summary>
        /// TODO：用于序列化SyncVar 自动生成
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="start"></param>
        protected virtual void SerializeSyncVars(NetworkWriter writer, bool start)
        {
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="serialize"></param>
        /// <returns></returns>
        internal bool Deserialize(NetworkReader reader, bool serialize)
        {
            bool result = true;
            byte safety = reader.ReadByte();
            int chunkStart = reader.position;
            try
            {
                OnDeserialize(reader, serialize);
            }
            catch (Exception e)
            {
                Debug.LogError($"反序列化对象失败。对象名称：{name} 组件：{GetType()} 场景Id：{@object.sceneId:X}\n{e}");
                result = false;
            }

            int size = reader.position - chunkStart;
            byte sizeHash = (byte)(size & 0xFF);
            if (sizeHash != safety)
            {
                Debug.LogWarning($"反序列化大小不匹配，请确保读取的数据量相同。读取字节：{size} bytes 哈希对比：{sizeHash}/{safety}");
                int correctedSize = ErrorCorrection(size, safety);
                reader.position = chunkStart + correctedSize;
                result = false;
            }

            return result;
        }

        /// <summary>
        /// 错误修正
        /// </summary>
        /// <param name="size"></param>
        /// <param name="safety"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ErrorCorrection(int size, byte safety)
        {
            uint cleared = (uint)size & 0xFFFFFF00;
            return (int)(cleared | safety);
        }

        /// <summary>
        /// 可以重写这个方法
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="start"></param>
        protected virtual void OnDeserialize(NetworkReader reader, bool start) => DeserializeSyncVars(reader, start);

        /// <summary>
        /// TODO：用于序列化SyncVar 自动生成
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="start"></param>
        protected virtual void DeserializeSyncVars(NetworkReader reader, bool start)
        {
        }
    }

    /// <summary>
    /// 远程调用模块
    /// </summary>
    public abstract partial class NetworkBehaviour
    {
        /// <summary>
        /// TODO:自动生成代码调用服务器Rpc
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="methodHash">方法哈希值</param>
        /// <param name="writer">写入器</param>
        /// <param name="channel">传输通道</param>
        protected void SendServerRpcInternal(string methodName, int methodHash, NetworkWriter writer, sbyte channel)
        {
            if (!NetworkManager.Client.isActive)
            {
                Debug.LogError($"调用 {methodName} 但是客户端不是活跃的。", gameObject);
                return;
            }

            if (!NetworkManager.Client.isReady)
            {
                Debug.LogWarning($"调用 {methodName} 但是客户端没有准备就绪的。对象名称：{name}", gameObject);
                return;
            }

            if (!isOwner)
            {
                Debug.LogWarning($"调用 {methodName} 但是客户端没有对象权限。对象名称：{name}", gameObject);
                return;
            }

            if (NetworkManager.Client.connection == null)
            {
                Debug.LogError($"调用 {methodName} 但是客户端的连接为空。对象名称：{name}", gameObject);
                return;
            }

            var message = new ServerRpcMessage
            {
                objectId = objectId,
                serialId = serialId,
                methodHash = (ushort)methodHash,
                segment = writer.ToArraySegment()
            };

            NetworkManager.Client.connection.Send(message, (Channel)channel);
        }


        /// <summary>
        /// TODO:自动生成代码调用客户端Rpc
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="methodHash">方法哈希值</param>
        /// <param name="writer">写入器</param>
        /// <param name="channel">传输通道</param>
        protected void SendClientRpcInternal(string methodName, int methodHash, NetworkWriter writer, sbyte channel)
        {
            if (!NetworkManager.Server.isActive)
            {
                Debug.LogError($"调用 {methodName} 但是服务器不是活跃的。", gameObject);
                return;
            }

            if (!isServer)
            {
                Debug.LogWarning($"调用 {methodName} 但是对象没被创建。对象名称：{name}。", gameObject);
                return;
            }

            var message = new ClientRpcMessage
            {
                objectId = objectId,
                serialId = serialId,
                methodHash = (ushort)methodHash,
                segment = writer.ToArraySegment()
            };

            using var writerObject = NetworkWriter.Pop();
            writerObject.Write(message);

            foreach (var client in NetworkManager.Server.clients.Values.Where(client => client.isReady))
            {
                client.InvokeRpc(message, (Channel)channel);
            }
        }

        /// <summary>
        /// TODO:自动生成代码调用指定客户端Rpc
        /// </summary>
        /// <param name="client">传入指定客户端</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="methodHash">方法哈希值</param>
        /// <param name="writer">写入器</param>
        /// <param name="channel">传输通道</param>
        protected void SendTargetRpcInternal(NetworkClient client, string methodName, int methodHash, NetworkWriter writer, sbyte channel)
        {
            if (!NetworkManager.Server.isActive)
            {
                Debug.LogError($"调用 {methodName} 但是服务器不是活跃的。", gameObject);
                return;
            }

            if (!isServer)
            {
                Debug.LogWarning($"调用 {methodName} 但是对象没被创建。对象名称：{name}。", gameObject);
                return;
            }

            client ??= connection;

            if (client is null)
            {
                Debug.LogError($"调用 {methodName} 但是对象的连接为空。对象名称：{name}", gameObject);
                return;
            }

            var message = new ClientRpcMessage
            {
                objectId = objectId,
                serialId = serialId,
                methodHash = (ushort)methodHash,
                segment = writer.ToArraySegment()
            };

            client.InvokeRpc(message, (Channel)channel);
        }
    }

    /// <summary>
    /// 网络变量模块
    /// </summary>
    public abstract partial class NetworkBehaviour
    {
        /// <summary>
        /// 添加标准的标准变量设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarSetterGeneral<T>(T value, ref T field, ulong dirty, Action<T, T> OnChanged)
        {
            if (!SyncVarEqualGeneral(value, ref field))
            {
                T oldValue = field;
                SetSyncVarGeneral(value, ref field, dirty);
                if (OnChanged != null)
                {
                    if (NetworkManager.Instance.mode == NetworkMode.Host && !GetSyncVarHook(dirty))
                    {
                        SetSyncVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetSyncVarHook(dirty, false);
                    }
                }
            }
        }

        /// <summary>
        /// 添加标准网络变量的访问器
        /// </summary>
        /// <param name="field"></param>
        /// <param name="OnChanged"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarGetterGeneral<T>(ref T field, Action<T, T> OnChanged, T value)
        {
            T oldValue = field;
            field = value;
            if (OnChanged != null && !SyncVarEqualGeneral(oldValue, ref field))
            {
                OnChanged(oldValue, field);
            }
        }

        /// <summary>
        /// 标准网络变量的值比较器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static bool SyncVarEqualGeneral<T>(T value, ref T field)
        {
            return EqualityComparer<T>.Default.Equals(value, field);
        }

        /// <summary>
        /// 标准网络变量的设置
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <typeparam name="T"></typeparam>
        // ReSharper disable once RedundantAssignment
        private void SetSyncVarGeneral<T>(T value, ref T field, ulong dirty)
        {
            SetSyncVarDirty(dirty);
            field = value;
        }

        /// <summary>
        /// 添加游戏对象的网络变量的设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <param name="objectId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarSetterGameObject(GameObject value, ref GameObject field, ulong dirty, Action<GameObject, GameObject> OnChanged,
            ref uint objectId)
        {
            if (!SyncVarEqualGameObject(value, objectId))
            {
                GameObject oldValue = field;
                SetSyncVarGameObject(value, ref field, dirty, ref objectId);
                if (OnChanged != null)
                {
                    if (NetworkManager.Instance.mode == NetworkMode.Host && !GetSyncVarHook(dirty))
                    {
                        SetSyncVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetSyncVarHook(dirty, false);
                    }
                }
            }
        }

        /// <summary>
        /// 添加游戏对象的网络变量的访问器
        /// </summary>
        /// <param name="field"></param>
        /// <param name="OnChanged"></param>
        /// <param name="reader"></param>
        /// <param name="objectId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarGetterGameObject(ref GameObject field, Action<GameObject, GameObject> OnChanged, NetworkReader reader,
            ref uint objectId)
        {
            uint oldId = objectId;
            GameObject oldObject = field;
            objectId = reader.ReadUInt();
            field = GetSyncVarGameObject(objectId, ref field);
            if (OnChanged != null && !SyncVarEqualGeneral(oldId, ref objectId))
            {
                OnChanged(oldObject, field);
            }
        }

        /// <summary>
        /// 游戏对象的网络变量的比较器
        /// </summary>
        /// <param name="newObject"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        private static bool SyncVarEqualGameObject(GameObject newObject, uint objectId)
        {
            uint newId = 0;
            if (newObject != null)
            {
                if (newObject.TryGetComponent(out NetworkObject @object))
                {
                    newId = @object.objectId;
                    if (newId == 0)
                    {
                        Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{newObject.name}");
                    }
                }
            }

            return newId == objectId;
        }

        /// <summary>
        /// 获取游戏对象的网络变量
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        private GameObject GetSyncVarGameObject(uint objectId, ref GameObject field)
        {
            if (isServer || !isClient)
            {
                return field;
            }

            if (NetworkManager.Client.spawns.TryGetValue(objectId, out var oldObject) && oldObject != null)
            {
                return field = oldObject.gameObject;
            }

            return null;
        }

        /// <summary>
        /// 设置游戏对象的网络变量
        /// </summary>
        /// <param name="newObject"></param>
        /// <param name="objectField"></param>
        /// <param name="dirty"></param>
        /// <param name="objectId"></param>
        private void SetSyncVarGameObject(GameObject newObject, ref GameObject objectField, ulong dirty, ref uint objectId)
        {
            if (GetSyncVarHook(dirty)) return;
            uint newId = 0;
            if (newObject != null)
            {
                if (newObject.TryGetComponent(out NetworkObject entity))
                {
                    newId = entity.objectId;
                    if (newId == 0)
                    {
                        Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{newObject.name}");
                    }
                }
            }

            SetSyncVarDirty(dirty);
            objectField = newObject;
            objectId = newId;
        }

        /// <summary>
        /// 添加网络对象的网络变量的设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <param name="objectId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarSetterNetworkObject(NetworkObject value, ref NetworkObject field, ulong dirty,
            Action<NetworkObject, NetworkObject> OnChanged, ref uint objectId)
        {
            if (!SyncVarEqualNetworkObject(value, objectId))
            {
                NetworkObject oldValue = field;
                SetSyncVarNetworkObject(value, ref field, dirty, ref objectId);
                if (OnChanged != null)
                {
                    if (NetworkManager.Instance.mode == NetworkMode.Host && !GetSyncVarHook(dirty))
                    {
                        SetSyncVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetSyncVarHook(dirty, false);
                    }
                }
            }
        }

        /// <summary>
        /// 添加网络对象的网络变量的访问器
        /// </summary>
        /// <param name="field"></param>
        /// <param name="OnChanged"></param>
        /// <param name="reader"></param>
        /// <param name="objectId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarGetterNetworkObject(ref NetworkObject field, Action<NetworkObject, NetworkObject> OnChanged,
            NetworkReader reader, ref uint objectId)
        {
            uint oldId = objectId;
            NetworkObject oldObject = field;
            objectId = reader.ReadUInt();
            field = GetSyncVarNetworkObject(objectId, ref field);
            if (OnChanged != null && !SyncVarEqualGeneral(oldId, ref objectId))
            {
                OnChanged(oldObject, field);
            }
        }

        /// <summary>
        /// 网络对象的网络变量的比较器
        /// </summary>
        /// <param name="object"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        private static bool SyncVarEqualNetworkObject(NetworkObject @object, uint objectId)
        {
            uint newId = 0;
            if (@object != null)
            {
                newId = @object.objectId;
                if (newId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            return newId == objectId;
        }

        /// <summary>
        /// 获取网络对象的网络变量
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="object"></param>
        /// <returns></returns>
        private NetworkObject GetSyncVarNetworkObject(uint objectId, ref NetworkObject @object)
        {
            if (isServer || !isClient) return @object;
            NetworkManager.Client.spawns.TryGetValue(objectId, out @object);
            return @object;
        }


        /// <summary>
        /// 设置网络对象的网络变量
        /// </summary>
        /// <param name="object"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="objectId"></param>
        private void SetSyncVarNetworkObject(NetworkObject @object, ref NetworkObject field, ulong dirty, ref uint objectId)
        {
            if (GetSyncVarHook(dirty)) return;
            uint newId = 0;
            if (@object != null)
            {
                newId = @object.objectId;
                if (newId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            SetSyncVarDirty(dirty);
            objectId = newId;
            field = @object;
        }

        /// <summary>
        /// 添加网络实体的网络变量的设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <param name="objectId"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarSetterNetworkBehaviour<T>(T value, ref T field, ulong dirty, Action<T, T> OnChanged, ref NetworkValue objectId)
            where T : NetworkBehaviour
        {
            if (!SyncVarEqualNetworkBehaviour(value, objectId))
            {
                T oldValue = field;
                SetSyncVarNetworkBehaviour(value, ref field, dirty, ref objectId);
                if (OnChanged != null)
                {
                    if (NetworkManager.Instance.mode == NetworkMode.Host && !GetSyncVarHook(dirty))
                    {
                        SetSyncVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetSyncVarHook(dirty, false);
                    }
                }
            }
        }

        /// <summary>
        /// 添加网络实体的网络变量的访问器
        /// </summary>
        /// <param name="field"></param>
        /// <param name="OnChanged"></param>
        /// <param name="reader"></param>
        /// <param name="objectId"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarGetterNetworkBehaviour<T>(ref T field, Action<T, T> OnChanged, NetworkReader reader, ref NetworkValue objectId)
            where T : NetworkBehaviour
        {
            NetworkValue oldId = objectId;
            T oldObject = field;
            objectId = reader.ReadNetworkValue();
            field = GetSyncVarNetworkBehaviour(objectId, ref field);
            if (OnChanged != null && !SyncVarEqualGeneral(oldId, ref objectId))
            {
                OnChanged(oldObject, field);
            }
        }

        /// <summary>
        /// 网络实体的网络变量的比较器
        /// </summary>
        /// <param name="object"></param>
        /// <param name="objectId"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static bool SyncVarEqualNetworkBehaviour<T>(T @object, NetworkValue objectId) where T : NetworkBehaviour
        {
            uint newId = 0;
            byte index = 0;
            if (@object != null)
            {
                newId = @object.objectId;
                index = @object.serialId;
                if (newId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            return objectId.Equals(newId, index);
        }

        /// <summary>
        /// 获取网络实体的网络变量
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSyncVarNetworkBehaviour<T>(NetworkValue value, ref T field) where T : NetworkBehaviour
        {
            if (isServer || !isClient)
            {
                return field;
            }

            if (!NetworkManager.Client.spawns.TryGetValue(value.objectId, out var oldObject))
            {
                return null;
            }

            field = (T)oldObject.entities[value.serialId];
            return field;
        }

        /// <summary>
        /// 设置网络实体的网络变量
        /// </summary>
        /// <param name="object"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="netId"></param>
        /// <typeparam name="T"></typeparam>
        private void SetSyncVarNetworkBehaviour<T>(T @object, ref T field, ulong dirty, ref NetworkValue netId) where T : NetworkBehaviour
        {
            if (GetSyncVarHook(dirty)) return;

            uint newId = 0;
            byte index = 0;
            if (@object != null)
            {
                newId = @object.objectId;
                index = @object.serialId;
                if (newId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            netId = new NetworkValue(newId, index);
            SetSyncVarDirty(dirty);
            field = @object;
        }
    }
}