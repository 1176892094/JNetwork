using System;
using System.Runtime.CompilerServices;
using JFramework.Interface;
using UnityEngine;
// ReSharper disable All

namespace JFramework.Net
{
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
}