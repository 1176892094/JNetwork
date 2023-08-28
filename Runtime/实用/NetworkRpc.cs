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
        private static readonly Dictionary<ushort, RpcMessage> messages = new Dictionary<ushort, RpcMessage>();

        /// <summary>
        /// TODO:自动生成代码注册服务器远程调用
        /// </summary>
        /// <param name="component"></param>
        /// <param name="methodName"></param>
        /// <param name="func"></param>
        public static void RegisterServerRpc(Type component, string methodName, RpcDelegate func)
        {
            RegisterRpc(component, methodName, RpcType.ServerRpc, func);
        }

        /// <summary>
        /// TODO:自动生成代码注册客户端远程调用
        /// </summary>
        /// <param name="component"></param>
        /// <param name="methodName"></param>
        /// <param name="func"></param>
        public static void RegisterClientRpc(Type component, string methodName, RpcDelegate func)
        {
            RegisterRpc(component, methodName, RpcType.ClientRpc, func);
        }

        /// <summary>
        /// 注册远程调用事件
        /// </summary>
        /// <param name="component"></param>
        /// <param name="methodName"></param>
        /// <param name="rpcType"></param>
        /// <param name="func"></param>
        private static void RegisterRpc(Type component, string methodName, RpcType rpcType, RpcDelegate func)
        {
            ushort hash = (ushort)(NetworkMessage.GetHashByName(methodName) & 0xFFFF);

            if (IsValidRpc(component, hash, rpcType, func))
            {
                return;
            }

            messages[hash] = new RpcMessage
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
        private static bool IsValidRpc(Type component, ushort methodHash, RpcType rpcType, RpcDelegate func)
        {
            if (messages.TryGetValue(methodHash, out var message))
            {
                if (message.Compare(component, rpcType, func))
                {
                    return true;
                }

                Debug.LogError($"{message.component} {message.function.Method.Name} 与 {component} {func.Method.Name} 具有相同的哈希值。请重新命名方法。");
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
            return messages.TryGetValue(hash, out var message) && message is { rpcType: RpcType.ServerRpc };
        }

        /// <summary>
        /// 调用远程函数
        /// </summary>
        /// <returns>返回是否调用成功</returns>
        internal static bool Invoke(ushort hash, RpcType rpcType, NetworkReader reader, NetworkBehaviour behaviour, UdpClient client = null)
        {
            if (!messages.TryGetValue(hash, out var message) || message == null || message.rpcType != rpcType) // 没有注册进字典
            {
                return false;
            }

            if (!message.component.IsInstanceOfType(behaviour)) // 判断是否是NetworkBehaviour的实例或派生类型的实例
            {
                return false;
            }

            message.function(behaviour, reader, client);
            return true;
        }

        /// <summary>
        /// 网络事件
        /// </summary>
        private sealed class RpcMessage
        {
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