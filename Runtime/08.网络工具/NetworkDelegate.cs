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
    public delegate void InvokeDelegate(NetworkBehaviour behaviour, NetworkReader reader, NetworkClient client);

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
        /// <param name="channel"></param>
        public static void RegisterServerRpc(Type component, int channel, string name, InvokeDelegate func)
        {
            RegisterInvoke(component, channel, name, InvokeMode.ServerRpc, func);
        }

        /// <summary>
        /// TODO:自动生成代码注册客户端远程调用
        /// </summary>
        /// <param name="component"></param>
        /// <param name="name"></param>
        /// <param name="func"></param>
        /// <param name="channel"></param>
        public static void RegisterClientRpc(Type component, int channel, string name, InvokeDelegate func)
        {
            RegisterInvoke(component, channel, name, InvokeMode.ClientRpc, func);
        }

        /// <summary>
        /// 注册远程调用事件
        /// </summary>
        /// <param name="component"></param>
        /// <param name="name"></param>
        /// <param name="mode"></param>
        /// <param name="func"></param>
        /// <param name="channel"></param>
        private static void RegisterInvoke(Type component, int channel, string name, InvokeMode mode, InvokeDelegate func)
        {
            var id = (ushort)(NetworkUtility.GetStableId(name) & 0xFFFF);
            if (!messages.TryGetValue(id, out var message))
            {
                message = new InvokeData
                {
                    channel = channel,
                    component = component,
                    mode = mode,
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
        internal static bool RequireReady(ushort id)
        {
            if (messages.TryGetValue(id, out var message) && message != null)
            {
                if ((message.channel & Channel.NonOwner) == 0 && message.mode == InvokeMode.ServerRpc)
                {
                    return true;
                }
            }

            return false;
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
            public int channel;
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