using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        private static void RegisterMessage()
        {
            NetworkEvent.RegisterMessage<CommandMessage>(OnCommandMessage);
            NetworkEvent.RegisterMessage<PingMessage>(OnPingMessage, false);
        }

        /// <summary>
        /// 当发送一条命令到Transport
        /// </summary>
        private static void OnCommandMessage(ClientEntity client, CommandMessage message, Channel channel)
        {
            if (!client.isReady)
            {
                Debug.LogWarning("Command received while client is not ready.");
                return;
            }

            if (!spawns.TryGetValue(message.netId, out var identity))
            {
                Debug.LogWarning($"Spawned object not found when handling Command message netId = {message.netId}");
                return;
            }

            if (RpcUtils.GetAuthorityByHash(message.functionHash) && identity.client != client)
            {
                Debug.LogWarning($"Command for object without authority netId = {message.netId}");
                return;
            }

            using var reader = NetworkReaderPool.Pop(message.payload);
            identity.HandleRpcEvent(message.componentIndex, message.functionHash, RpcType.ServerRpc, reader, client);
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