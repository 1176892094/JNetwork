// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-06-09  15:06
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkDelegate
    {
        /// <summary>
        /// 远程调用事件字典
        /// </summary>
        private static readonly Dictionary<ushort, InvokeData> messages = new Dictionary<ushort, InvokeData>();

        /// <summary>
        /// TODO:自动生成代码注册服务器远程调用
        /// </summary>
        /// <param name="component"></param>
        /// <param name="name"></param>
        /// <param name="func"></param>
        public static void RegisterServerRpc(Type component, string name, InvokeDelegate func)
        {
            RegisterInvoke(component, name, InvokeMode.ServerRpc, func);
        }

        /// <summary>
        /// TODO:自动生成代码注册客户端远程调用
        /// </summary>
        /// <param name="component"></param>
        /// <param name="name"></param>
        /// <param name="func"></param>
        public static void RegisterClientRpc(Type component, string name, InvokeDelegate func)
        {
            RegisterInvoke(component, name, InvokeMode.ClientRpc, func);
        }

        /// <summary>
        /// 注册远程调用事件
        /// </summary>
        /// <param name="component"></param>
        /// <param name="name"></param>
        /// <param name="mode"></param>
        /// <param name="func"></param>
        private static void RegisterInvoke(Type component, string name, InvokeMode mode, InvokeDelegate func)
        {
            var id = (ushort)(NetworkUtility.GetHashToName(name) & 0xFFFF);
            if (!messages.TryGetValue(id, out var message))
            {
                message = new InvokeData
                {
                    mode = mode,
                    component = component,
                    func = func,
                };
                messages[id] = message;
            }

            if (!message.Compare(component, mode, func))
            {
                Debug.LogError($"网络调用 {message.component} {message.func.Method.Name} 与 {component} {func.Method.Name} 产生冲突。");
            }
        }


        /// <summary>
        /// 判断调用是否需要权限
        /// </summary>
        /// <param name="id">传入方法的hash值</param>
        /// <returns>返回是否需要权限</returns>
        internal static bool Contains(ushort id)
        {
            return messages.TryGetValue(id, out var message) && message is { mode: InvokeMode.ServerRpc };
        }

        /// <summary>
        /// 调用远程函数
        /// </summary>
        /// <returns>返回是否调用成功</returns>
        internal static bool Invoke(ushort id, InvokeMode mode, NetworkClient client, NetworkReader reader, NetworkBehaviour component)
        {
            if (!messages.TryGetValue(id, out var message) || message == null || message.mode != mode)
            {
                return false;
            }

            if (!message.component.IsInstanceOfType(component)) // 判断是否是NetworkBehaviour的实例或派生类型的实例
            {
                return false;
            }
            
            message.func.Invoke(component, reader, client);
            return true;
        }

        /// <summary>
        /// 网络事件
        /// </summary>
        private class InvokeData
        {
            public Type component;
            public InvokeMode mode;
            public InvokeDelegate func;

            public bool Compare(Type component, InvokeMode mode, InvokeDelegate func)
            {
                return this.component == component && this.mode == mode && this.func == func;
            }
        }
    }
}