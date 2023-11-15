using System;
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
                OnClientConnect(new UnityClient(clientId));
            }
        }

        /// <summary>
        /// 当客户端连接到服务器
        /// </summary>
        /// <param name="client">连接的客户端实体</param>
        internal static void OnClientConnect(UnityClient client)
        {
            clients.TryAdd(client.clientId, client);
            if (client.clientId == NetworkConst.HostId)
            {
                connection = client;
            }

            client.isSpawn = true;
            OnServerConnect?.Invoke(client);
        }

        /// <summary>
        /// 服务器断开指定客户端连接
        /// </summary>
        /// <param name="clientId"></param>
        internal static void OnServerDisconnected(int clientId)
        {
            if (clients.TryGetValue(clientId, out var client))
            {
                OnServerDisconnect?.Invoke(client);
                var copyList = spawns.Values.Where(@object => @object.connection == client).ToList();
                foreach (var @object in copyList)
                {
                    Destroy(@object);
                }

                if (client.clientId == NetworkConst.HostId)
                {
                    connection = null;
                }

                clients.Remove(client.clientId);
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
                Debug.LogWarning($"无法将读取消息合批!。断开客户端：{client}");
                client.Disconnect();
                return;
            }

            while (!isLoadScene && client.readerPack.ReadDequeue(out var reader, out double remoteTime))
            {
                if (reader.Residue < NetworkConst.MessageSize)
                {
                    Debug.LogError($"网络消息应该有个开始的Id。断开客户端：{client}");
                    client.Disconnect();
                    return;
                }

                client.remoteTime = remoteTime;

                if (!NetworkMessage.ReadMessage(reader, out ushort id))
                {
                    Debug.LogError($"无效的网络消息类型！断开客户端：{client}");
                    client.Disconnect();
                    return;
                }

                if (!messages.TryGetValue(id, out MessageDelegate handle))
                {
                    Debug.LogError($"未知的网络消息Id：{id} 断开客户端：{client}");
                    client.Disconnect();
                    return;
                }

                handle.Invoke(client, reader, channel);
            }

            if (!isLoadScene && client.readerPack.Count > 0)
            {
                Debug.LogError($"读取器合批之后仍然还有次数残留！残留次数：{client.readerPack.Count}");
            }
        }
    }
}