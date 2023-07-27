using System;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        /// <summary>
        /// 添加传输事件
        /// </summary>
        private static void RegisterTransport()
        {
            Transport.OnClientConnected += OnClientConnected;
            Transport.OnClientDisconnected += OnClientDisconnected;
            Transport.OnClientReceive += OnClientReceive;
        }

        /// <summary>
        /// 移除传输事件
        /// </summary>
        private static void UnRegisterTransport()
        {
            Transport.OnClientConnected -= OnClientConnected;
            Transport.OnClientDisconnected -= OnClientDisconnected;
            Transport.OnClientReceive -= OnClientReceive;
        }

        /// <summary>
        /// 当客户端连接
        /// </summary>
        private static void OnClientConnected()
        {
            if (connection == null)
            {
                Debug.LogError("没有有效的服务器连接！");
                return;
            }

            Debug.Log("客户端连接成功。");
            state = ConnectState.Connected;
            OnClientConnect?.Invoke();
            NetworkTime.ResetStatic();
            NetworkTime.Update();
            Ready();
        }

        /// <summary>
        /// 当客户端断开连接
        /// </summary>
        private static void OnClientDisconnected()
        {
            if (!isActive) return;
            Debug.Log("客户端断开连接。");
            UnRegisterTransport();
            OnClientDisconnect?.Invoke();
            StopClient();
            state = ConnectState.Disconnected;
        }

        /// <summary>
        /// 当客户端从服务器接收消息
        /// </summary>
        /// <param name="data"></param>
        /// <param name="channel"></param>
        internal static void OnClientReceive(ArraySegment<byte> data, Channel channel)
        {
            if (connection == null)
            {
                Debug.LogError("没有连接到有效的服务器！");
                return;
            }

            if (!connection.readerPack.ReadEnqueue(data))
            {
                Debug.LogError($"无法将读取消息合批!");
                connection.Disconnect();
                return;
            }

            while (!isLoadScene && connection.readerPack.ReadDequeue(out var reader, out double remoteTime))
            {
                if (reader.Residue < NetworkConst.MessageSize)
                {
                    Debug.LogError($"网络消息应该有个开始的Id");
                    connection.Disconnect();
                    return;
                }

                connection.remoteTime = remoteTime;

                if (!NetworkMessage.ReadMessage(reader, out ushort id))
                {
                    Debug.LogError("无效的网络消息类型！");
                    connection.Disconnect();
                    return;
                }

                if (!messages.TryGetValue(id, out MessageDelegate handle))
                {
                    Debug.LogError($"未知的网络消息Id：{id}");
                    connection.Disconnect();
                    return;
                }

                handle.Invoke(connection, reader, channel);
            }

            if (!isLoadScene && connection.readerPack.Count > 0)
            {
                Debug.LogError($"读取器合批之后仍然还有次数残留！残留次数：{connection.readerPack.Count}\n");
            }
        }
    }
}