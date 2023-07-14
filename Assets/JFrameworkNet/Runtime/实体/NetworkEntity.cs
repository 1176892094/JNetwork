using System.Linq;
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
        public uint netId => @object.netId;
        
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
        public NetworkClientEntity connection => @object.connection;
        
        /// <summary>
        /// 当前实体在网络对象中的位置
        /// </summary>
        internal byte component;
        
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
    }
}