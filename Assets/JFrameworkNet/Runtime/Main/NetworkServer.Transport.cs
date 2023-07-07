using System;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        /// <summary>
        /// 添加传输事件
        /// </summary>
        private static void RegisterTransport()
        {
            UnRegisterTransport();
            Transport.OnServerConnected += OnServerConnected;
            Transport.OnServerDisconnected += OnServerDisconnected;
            Transport.OnServerReceive += OnServerReceive;
        }

        /// <summary>
        /// 移除传输事件
        /// </summary>
        private static void UnRegisterTransport()
        {
            Transport.OnServerConnected -= OnServerConnected;
            Transport.OnServerDisconnected -= OnServerDisconnected;
            Transport.OnServerReceive -= OnServerReceive;
        }

        /// <summary>
        /// 指定客户端连接到服务器
        /// </summary>
        /// <param name="clientId"></param>
        private static void OnServerConnected(int clientId)
        {
            if (clientId == 0)
            {
                Debug.LogError($"Invalid clientId: {clientId} .");
                Transport.current.ServerDisconnect(clientId);
                return;
            }

            if (clients.ContainsKey(clientId))
            {
                Transport.current.ServerDisconnect(clientId);
                return;
            }

            if (clients.Count >= maxConnection)
            {
                Transport.current.ServerDisconnect(clientId);
                return;
            }

            OnClientConnect(new ClientConnection(clientId));
        }

        /// <summary>
        /// 服务器断开指定客户端连接
        /// </summary>
        /// <param name="clientId"></param>
        private static void OnServerDisconnected(int clientId)
        {
            if (clients.TryGetValue(clientId, out var client))
            {
                OnDisconnected?.Invoke(client);
                clients.Remove(clientId);
            }
        }

        /// <summary>
        /// 服务器从传输接收数据
        /// </summary>
        private static void OnServerReceive(int clientId, ArraySegment<byte> segment, Channel channel)
        {
            if (!clients.TryGetValue(clientId, out var client))
            {
                Debug.LogError($"Receive data is unknown clientId: {clientId}");
                return;
            }

            if (!client.receive.ReadEnqueue(segment))
            {
                Debug.LogWarning($"Messages should start with message id");
                client.Disconnect();
                return;
            }

            while (!isLoadScene && client.receive.ReadDequeue(out var reader, out double timestamp))
            {
                if (reader.Residue >= NetworkConst.MessageSize)
                {
                    client.timestamp = timestamp;
                    if (!TryInvoke(client, reader, channel))
                    {
                        Debug.LogWarning($"Failed to unpack and invoke message. Disconnecting {clientId}.");
                        client.Disconnect();
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning($"Messages should start with message id. Disconnecting {clientId}");
                    client.Disconnect();
                    return;
                }
            }

            if (!isLoadScene && client.receive.Count > 0)
            {
                Debug.LogError($"Still had {client.receive.Count} batches remaining after processing.");
            }
        }

        /// <summary>
        /// 解码并且调用
        /// </summary>
        /// <returns>返回是否调用成功</returns>
        private static bool TryInvoke(ClientConnection client, NetworkReader reader, Channel channel)
        {
            if (NetworkUtils.ReadMessage(reader, out ushort id))
            {
                if (NetworkEvent.ServerMessage(id, client, reader, channel))
                {
                    return true;
                }

                Debug.LogWarning($"Unknown message id: {id}.");
                return false;
            }

            Debug.LogWarning($"Invalid message header.");
            return false;
        }
    }
}