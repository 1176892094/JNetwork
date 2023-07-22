using System;
using System.Collections.Generic;
using System.Linq;
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
                Debug.LogError($"无效的客户端连接。客户端：{clientId}");
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
                OnServerDisconnect?.Invoke(client);
                
                foreach (var @object in client.observers.ToArray())
                {
                    Destroy(@object);
                }

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
                Debug.LogError($"服务器接收到消息。未知的客户端：{clientId}");
                return;
            }

            if (!client.readerPack.ReadEnqueue(segment))
            {
                Debug.LogWarning($"网络消息应该有个开始的Id。断开客户端：{client}");
                client.Disconnect();
                return;
            }

            while (!isLoadScene && client.readerPack.ReadDequeue(out var reader, out double timestamp))
            {
                if (reader.Residue >= NetworkConst.EventSize)
                {
                    client.timestamp = timestamp;
                    if (!TryInvoke(client, reader, channel))
                    {
                        Debug.LogWarning($"无法解包调用网络信息。断开客户端：{client}");
                        client.Disconnect();
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning($"网络消息应该有个开始的Id。断开客户端：{client}");
                    client.Disconnect();
                    return;
                }
            }

            if (!isLoadScene && client.readerPack.Count > 0)
            {
                Debug.LogError($"读取器合批之后仍然还有次数残留！残留次数：{client.readerPack.Count}");
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
                if (events.TryGetValue(id, out EventDelegate handle))
                {
                    handle.Invoke(client, reader, channel);
                    return true;
                }

                Debug.LogWarning($"未知的网络消息Id：{id}");
                return false;
            }

            Debug.LogWarning($"无效的网络消息类型！");
            return false;
        }
    }
}