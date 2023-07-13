using System;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class ServerManager
    {
        /// <summary>
        /// 注册服务器消息事件
        /// </summary>
        private static void RegisterEvent()
        {
            Debug.Log("注册服务器事件");
            RegisterEvent<ServerRpcEvent>(OnServerRpcEvent);
            RegisterEvent<ReadyEvent>(NetworkManager.OnServerReadyEvent);
            RegisterEvent<PingEvent>(NetworkTime.OnPingEvent, false);
        }
        
        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterEvent<T>(Action<ClientEntity, T> handle, bool authority = true) where T : struct, IEvent
        {
            events[NetworkEvent<T>.Id] = NetworkEvent.Register(handle, authority);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterEvent<T>(Action<ClientEntity, T, Channel> handle, bool authority = true) where T : struct, IEvent
        {
            events[NetworkEvent<T>.Id] = NetworkEvent.Register(handle, authority);
        }

        /// <summary>
        /// 当发送一条命令到Transport
        /// </summary>
        private static void OnServerRpcEvent(ClientEntity client, ServerRpcEvent @event, Channel channel)
        {
            if (!client.isReady)
            {
                Debug.LogWarning("接收到 ServerRpc 但客户端没有准备就绪");
            }
            else if (!spawns.TryGetValue(@event.netId, out var @object))
            {
                Debug.LogWarning($"没有找到发送 ServerRpc 的对象。对象网络Id：{@event.netId}");
            }
            else if (RpcUtils.GetAuthorityByHash(@event.functionHash) && @object.connection != client)
            {
                Debug.LogWarning($"接收到 ServerRpc 但对象没有通过验证。对象网络Id：{@event.netId}");
            }
            else
            {
                using var reader = NetworkReader.Pop(@event.segment);
                @object.InvokeRpcEvent(@event.componentIndex, @event.functionHash, RpcType.ServerRpc, reader, client);
            }
        }
    }
}