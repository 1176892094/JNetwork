using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public abstract partial class NetworkEntity : MonoBehaviour, INetworkEvent
    {
        /// <summary>
        /// 服务器变量的改变选项
        /// </summary>
        protected ulong serverVarDirty;

        /// <summary>
        /// 服务器对象的改变选项
        /// </summary>
        internal ulong serverObjectDirty;

        /// <summary>
        /// 服务器变量的钩子
        /// </summary>
        private ulong serverVarHook;

        /// <summary>
        /// 网络对象组件
        /// </summary>
        internal NetworkObject @object;

        /// <summary>
        /// 同步模式
        /// </summary>
        internal SyncMode syncMode;

        /// <summary>
        /// 同步方向
        /// </summary>
        internal SyncDirection syncDirection;

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
        /// 网络对象连接的服务器(客户端不为空，服务器为空)
        /// </summary>
        public NetworkServerEntity server => @object.server;

        /// <summary>
        /// 网络对象连接的客户端(服务器不为空，客户端为空)
        /// </summary>
        public NetworkClientEntity connection => @object.client;

        /// <summary>
        /// 当前实体在网络对象中的位置
        /// </summary>
        internal byte serialId;

        /// <summary>
        /// 同步间隔
        /// </summary>
        private float syncInterval;

        /// <summary>
        /// 上一次同步时间
        /// </summary>
        private double lastSyncTime;

        private readonly List<SyncObject> syncObjects = new List<SyncObject>();

        /// <summary>
        /// 是否能够改变网络值
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDirty()
        {
            return (serverVarDirty | serverObjectDirty) != 0UL && NetworkTime.localTime - lastSyncTime >= syncInterval;
        }

        /// <summary>
        /// 设置网络变量值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDirty()
        {
            SetServerVarDirty(ulong.MaxValue);
        }

        /// <summary>
        /// 设置服务器变量改变
        /// </summary>
        /// <param name="dirty"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetServerVarDirty(ulong dirty) => serverVarDirty |= dirty;

        /// <summary>
        /// 获取服务器变量的钩子
        /// </summary>
        /// <param name="dirty"></param>
        /// <returns></returns>
        private bool GetServerVarHook(ulong dirty) => (serverVarHook & dirty) != 0UL;

        /// <summary>
        /// 设置服务器变量的钩子
        /// </summary>
        /// <param name="dirty"></param>
        /// <param name="value"></param>
        private void SetServerVarHook(ulong dirty, bool value)
        {
            serverVarHook = value ? serverVarHook | dirty : serverVarHook & ~dirty;
        }

        /// <summary>
        /// 网络变量序列化
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="init"></param>
        protected virtual void OnSerialize(NetworkWriter writer, bool init)
        {
            SerializeSyncObjects(writer, init);
            SerializeSyncVars(writer, init);
        }

        /// <summary>
        /// 序列化网络对象
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="init"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SerializeSyncObjects(NetworkWriter writer, bool init)
        {
            if (init)
            {
                SerializeObjectsAll(writer);
            }
            else
            {
                SerializeObjectsDelta(writer);
            }
        }

        /// <summary>
        /// 序列化所有网络对象
        /// </summary>
        /// <param name="writer"></param>
        private void SerializeObjectsAll(NetworkWriter writer)
        {
            foreach (var syncObject in syncObjects)
            {
                syncObject.OnSerializeAll(writer);
            }
        }

        /// <summary>
        /// 序列化指定网络对象
        /// </summary>
        /// <param name="writer"></param>
        private void SerializeObjectsDelta(NetworkWriter writer)
        {
            writer.WriteULong(serverObjectDirty);
            for (int i = 0; i < syncObjects.Count; i++)
            {
                SyncObject syncObject = syncObjects[i];
                if ((serverObjectDirty & (1UL << i)) != 0)
                {
                    syncObject.OnSerializeDelta(writer);
                }
            }
        }

        /// <summary>
        /// 序列化网络变量
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="initialState"></param>
        protected virtual void SerializeSyncVars(NetworkWriter writer, bool initialState)
        {
            //TODO：通过自动生成
        }

        /// <summary>
        /// 网络变量反序列化
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="init"></param>
        protected virtual void OnDeserialize(NetworkReader reader, bool init)
        {
            DeserializeSyncObjects(reader, init);
            DeserializeSyncVars(reader, init);
        }

        /// <summary>
        /// 反序列化网络对象
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="init"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DeserializeSyncObjects(NetworkReader reader, bool init)
        {
            if (init)
            {
                DeserializeObjectsAll(reader);
            }
            else
            {
                DeserializeObjectsDelta(reader);
            }
        }

        /// <summary>
        /// 反序列化所有网络对象
        /// </summary>
        /// <param name="reader"></param>
        private void DeserializeObjectsAll(NetworkReader reader)
        {
            foreach (var syncObject in syncObjects)
            {
                syncObject.OnDeserializeAll(reader);
            }
        }

        /// <summary>
        /// 反序列化指定网络对象
        /// </summary>
        /// <param name="reader"></param>
        private void DeserializeObjectsDelta(NetworkReader reader)
        {
            ulong dirty = reader.ReadULong();
            for (int i = 0; i < syncObjects.Count; i++)
            {
                SyncObject syncObject = syncObjects[i];
                if ((dirty & (1UL << i)) != 0)
                {
                    syncObject.OnDeserializeDelta(reader);
                }
            }
        }

        /// <summary>
        /// 反序列化网络变量
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="initialState"></param>
        protected virtual void DeserializeSyncVars(NetworkReader reader, bool initialState)
        {
            //TODO：通过自动生成
        }
    }
}