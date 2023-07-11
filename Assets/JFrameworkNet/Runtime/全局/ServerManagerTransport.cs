using System;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class ServerManager
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
            }
            else if (clients.ContainsKey(clientId))
            {
                Transport.current.ServerDisconnect(clientId);
            }
            else if (clients.Count >= maxConnection)
            {
                Transport.current.ServerDisconnect(clientId);
            }
            else
            {
                OnClientConnect(new ClientEntity(clientId));
            }
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
        internal static void OnServerReceive(int clientId, ArraySegment<byte> segment, Channel channel)
        {
            if (!clients.TryGetValue(clientId, out var client))
            {
                Debug.LogError($"Receive data is unknown clientId: {clientId}");
                return;
            }

            if (!client.readers.ReadEnqueue(segment))
            {
                Debug.LogWarning($"Messages should start with message id");
                client.Disconnect();
                return;
            }

            while (!isLoadScene && client.readers.ReadDequeue(out var reader, out double timestamp))
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

            if (!isLoadScene && client.readers.Count > 0)
            {
                Debug.LogError($"Still had {client.readers.Count} batches remaining after processing.");
            }
        }

        /// <summary>
        /// 尝试读取并调用从客户端接收的委托
        /// </summary>
        /// <param name="client">客户端的Id</param>
        /// <param name="reader">网络读取器</param>
        /// <param name="channel">传输通道</param>
        /// <returns>返回是否读取成功</returns>
        private static bool TryInvoke(ClientEntity client, NetworkReader reader, Channel channel)
        {
            if (NetworkEvent.ReadEvent(reader, out ushort id))
            {
                if (messages.TryGetValue(id, out EventDelegate handle))
                {
                    handle.Invoke(client, reader, channel);
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