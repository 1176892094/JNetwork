using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public abstract partial class NetworkBehaviour : MonoBehaviour, INetworkEvent
    {
        /// <summary>
        /// 服务器变量的改变选项
        /// </summary>
        private ulong syncVarDirty { get; set; }

        /// <summary>
        /// 服务器变量的钩子
        /// </summary>
        private ulong syncVarHook;

        /// <summary>
        /// 网络对象组件
        /// </summary>
        internal NetworkObject @object;

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
        public ServerEntity server => @object.server;

        /// <summary>
        /// 网络对象连接的客户端(服务器不为空，客户端为空)
        /// </summary>
        public ClientEntity connection => @object.client;

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

        /// <summary>
        /// 是否能够改变网络值
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDirty()
        {
            return syncVarDirty != 0UL && NetworkTime.localTime - lastSyncTime >= syncInterval;
        }

        /// <summary>
        /// 设置网络变量值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDirty()
        {
            SetSyncVarDirty(ulong.MaxValue);
        }
        
        public void ClearAllDirty()
        {
            lastSyncTime = NetworkTime.localTime;
            syncVarDirty= 0L;
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
        
        internal void Serialize(NetworkWriter writer, bool serialize)
        {
            int headerPosition = writer.position;
            writer.WriteByte(0);
            int contentPosition = writer.position;
            
            try
            {
                SerializeSyncVars(writer, serialize);
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
        /// 序列化网络变量
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="serialize"></param>
        protected virtual void SerializeSyncVars(NetworkWriter writer, bool serialize)
        {
            //TODO：通过自动生成
        }

        internal bool Deserialize(NetworkReader reader, bool serialize)
        {
            bool result = true;
            byte safety = reader.ReadByte();
            int chunkStart = reader.position;
            try
            {
                DeserializeSyncVars(reader, serialize);
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
                Debug.LogWarning($"反序列化大小不匹配，请确保读取的数据量相同。读取字节：{size} bytes 哈希对比：{sizeHash:X2}/{safety:X2}");
                int correctedSize = ErrorCorrection(size, safety);
                reader.position = chunkStart + correctedSize;
                result = false;
            }

            return result;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ErrorCorrection(int size, byte safety)
        {
            uint cleared = (uint)size & 0xFFFFFF00;
            return (int)(cleared | safety);
        }

        /// <summary>
        /// 反序列化网络变量
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="serialize"></param>
        protected virtual void DeserializeSyncVars(NetworkReader reader, bool serialize)
        {
            //TODO：通过自动生成
        }
    }
}