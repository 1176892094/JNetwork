using System;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        private static void RegisterEvent()
        {
            RegisterEvent<CommandMessage>(OnCommandMessage);
            RegisterEvent<PingMessage>(OnPingMessage, false);
        }
        
        /// <summary>
        /// 注册网络消息
        /// </summary>
        internal static void RegisterEvent<T>(Action<ClientEntity, T> handle, bool isAuthority = true) where T : struct, IEvent
        {
            messages[MessageId<T>.Id] = NetworkUtils.Register(handle, isAuthority);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterEvent<T>(Action<ClientEntity, T, Channel> handle, bool isAuthority = true) where T : struct, IEvent
        {
            messages[MessageId<T>.Id] = NetworkUtils.Register(handle, isAuthority);
        }

        /// <summary>
        /// 当发送一条命令到Transport
        /// </summary>
        private static void OnCommandMessage(ClientEntity client, CommandMessage message, Channel channel)
        {
            if (!client.isReady)
            {
                Debug.LogWarning("Command received while client is not ready.");
            }
            else if (!spawns.TryGetValue(message.netId, out var @object))
            {
                Debug.LogWarning($"Spawned object not found Command message netId = {message.netId}");
            }
            else if (RpcUtils.GetAuthorityByHash(message.functionHash) && @object.client != client)
            {
                Debug.LogWarning($"Command for object without authority netId = {message.netId}");
            }
            else
            {
                using var reader = NetworkReader.Pop(message.payload);
                @object.HandleRpcEvent(message.componentIndex, message.functionHash, RpcType.ServerRpc, reader, client);
            }
        }

        private static void OnPingMessage(ClientEntity client, PingMessage message)
        {
            PongMessage pongMessage = new PongMessage
            {
                clientTime = message.clientTime,
            };
            client.Send(pongMessage, Channel.Unreliable);
        }
    }
}