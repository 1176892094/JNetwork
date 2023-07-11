using System;
using JFramework.Interface;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        /// <summary>
        /// 注册服务器消息事件
        /// </summary>
        private static void RegisterEvent()
        {
            RegisterEvent<CommandEvent>(OnCommandEvent);
            RegisterEvent<PingEvent>(OnPingEvent, false);
        }
        
        /// <summary>
        /// 注册网络消息
        /// </summary>
        internal static void RegisterEvent<T>(Action<ClientEntity, T> handle, bool authority = true) where T : struct, IEvent
        {
            messages[EventId<T>.Id] = NetworkEvent.Register(handle, authority);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterEvent<T>(Action<ClientEntity, T, Channel> handle, bool authority = true) where T : struct, IEvent
        {
            messages[EventId<T>.Id] = NetworkEvent.Register(handle, authority);
        }

        /// <summary>
        /// 当发送一条命令到Transport
        /// </summary>
        private static void OnCommandEvent(ClientEntity client, CommandEvent @event, Channel channel)
        {
            if (!client.isReady)
            {
                Debug.LogWarning("Command received while client is not ready.");
            }
            else if (!spawns.TryGetValue(@event.netId, out var @object))
            {
                Debug.LogWarning($"Spawned object not found Command message netId = {@event.netId}");
            }
            else if (RpcUtils.GetAuthorityByHash(@event.functionHash) && @object.connection != client)
            {
                Debug.LogWarning($"Command for object without authority netId = {@event.netId}");
            }
            else
            {
                using var reader = NetworkReader.Pop(@event.payload);
                @object.HandleRpcEvent(@event.componentIndex, @event.functionHash, RpcType.ServerRpc, reader, client);
            }
        }

        /// <summary>
        /// Ping的事件
        /// </summary>
        private static void OnPingEvent(ClientEntity client, PingEvent @event)
        {
            PongEvent pongEvent = new PongEvent
            {
                clientTime = @event.clientTime,
            };
            client.Send(pongEvent, Channel.Unreliable);
        }
    }
}