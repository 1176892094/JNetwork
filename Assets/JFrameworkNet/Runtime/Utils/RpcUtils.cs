using System;
using System.Collections.Generic;

namespace JFramework.Net
{
    public static class RpcUtils
    {
        private static readonly Dictionary<ushort, Invoker> delegates = new Dictionary<ushort, Invoker>();

        /// <summary>
        /// 判断调用是否需要权限
        /// </summary>
        /// <param name="hash">传入方法的hash值</param>
        /// <returns>返回是否需要权限</returns>
        internal static bool GetAuthorityByHash(ushort hash)
        {
            return TryGetInvoker(hash, RpcType.ServerRpc, out var invoker) && invoker.authority;
        }
        
        /// <summary>
        /// 调用远程函数
        /// </summary>
        /// <returns>返回是否调用成功</returns>
        internal static bool Invoke(ushort hash, RpcType rpcType, NetworkReader reader, NetworkBehaviour component, ClientConnection client = null)
        {
            if (!TryGetInvoker(hash, rpcType, out var invoker)) return false;
            if (!invoker.component.IsInstanceOfType(component)) return false; // 判断是否是NetworkBehaviour的实例或派生类型的实例
            invoker.function(component, reader, client);
            return true;
        }

        /// <summary>
        /// 判断是否包含调用方法
        /// </summary>
        /// <returns>返回得到方法并且是相同的Rpc类型</returns>
        private static bool TryGetInvoker(ushort hash, RpcType rpc, out Invoker invoker)
        {
            return delegates.TryGetValue(hash, out invoker) && invoker != null && invoker.rpcType == rpc;
        }

        private sealed class Invoker
        {
            public bool authority;
            public Type component;
            public RpcType rpcType;
            public RpcDelegate function;

            public bool AreEqual(Type component, RpcType rpcType, RpcDelegate function)
            {
                return this.component == component && this.rpcType == rpcType && this.function == function;
            }
        }
    }
}