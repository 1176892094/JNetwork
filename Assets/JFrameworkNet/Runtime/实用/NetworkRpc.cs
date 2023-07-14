using System;
using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkRpc
    {
        /// <summary>
        /// 远程调用事件字典
        /// </summary>
        private static readonly Dictionary<ushort, NetworkEvent> rpcEvents = new Dictionary<ushort, NetworkEvent>();

        /// <summary>
        /// TODO:自动生成代码注册服务器远程调用
        /// </summary>
        /// <param name="component"></param>
        /// <param name="methodName"></param>
        /// <param name="func"></param>
        public static void RegisterServerRpc(Type component, string methodName, RpcDelegate func)
        {
            RegisterRpcEvent(component, methodName, RpcType.ServerRpc, func);
        }

        /// <summary>
        /// TODO:自动生成代码注册客户端远程调用
        /// </summary>
        /// <param name="component"></param>
        /// <param name="methodName"></param>
        /// <param name="func"></param>
        public static void RegisterClientRpc(Type component, string methodName, RpcDelegate func)
        {
            RegisterRpcEvent(component, methodName, RpcType.ClientRpc, func);
        }

        /// <summary>
        /// 注册远程调用事件
        /// </summary>
        /// <param name="component"></param>
        /// <param name="methodName"></param>
        /// <param name="rpcType"></param>
        /// <param name="func"></param>
        private static void RegisterRpcEvent(Type component, string methodName, RpcType rpcType, RpcDelegate func)
        {
            ushort hash = (ushort)(Net.NetworkEvent.GetHashByName(methodName) & 0xFFFF);

            if (IsValidRpcEvent(component, hash, rpcType, func))
            {
                return;
            }

            rpcEvents[hash] = new NetworkEvent
            {
                rpcType = rpcType,
                component = component,
                function = func,
            };
        }

        /// <summary>
        /// 判断远程事件是否有效
        /// </summary>
        /// <param name="component"></param>
        /// <param name="methodHash"></param>
        /// <param name="rpcType"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        private static bool IsValidRpcEvent(Type component, ushort methodHash, RpcType rpcType, RpcDelegate func)
        {
            if (rpcEvents.TryGetValue(methodHash, out var @event))
            {
                if (@event.Compare(component, rpcType, func))
                {
                    return true;
                }

                Debug.LogError($"{@event.component} {@event.function.Method.Name} 与 {component} {func.Method.Name} 具有相同的哈希值。请重新命名方法。");
            }

            return false;
        }
        
        
        /// <summary>
        /// 判断调用是否需要权限
        /// </summary>
        /// <param name="hash">传入方法的hash值</param>
        /// <returns>返回是否需要权限</returns>
        internal static bool HasAuthority(ushort hash)
        {
            return rpcEvents.TryGetValue(hash, out var @event) && @event is { rpcType: RpcType.ServerRpc };
        }

        
        /// <summary>
        /// 调用远程函数
        /// </summary>
        /// <returns>返回是否调用成功</returns>
        internal static bool Invoke(ushort hash, RpcType rpcType, NetworkReader reader, NetworkEntity entity, NetworkClientEntity client = null)
        {
            if (!TryGetInvoker(hash, rpcType, out var @event)) return false;
            if (!@event.component.IsInstanceOfType(entity)) return false; // 判断是否是NetworkBehaviour的实例或派生类型的实例
            @event.function(entity, reader, client);
            return true;
        }

        /// <summary>
        /// 判断是否包含调用方法
        /// </summary>
        /// <returns>返回得到方法并且是相同的Rpc类型</returns>
        private static bool TryGetInvoker(ushort hash, RpcType rpc, out NetworkEvent @event)
        {
            return rpcEvents.TryGetValue(hash, out @event) && @event != null && @event.rpcType == rpc;
        }

        /// <summary>
        /// 网络事件
        /// </summary>
        private sealed class NetworkEvent
        {
            public bool authority;
            public Type component;
            public RpcType rpcType;
            public RpcDelegate function;

            public bool Compare(Type component, RpcType rpcType, RpcDelegate function)
            {
                return this.component == component && this.rpcType == rpcType && this.function == function;
            }
        }
    }
}