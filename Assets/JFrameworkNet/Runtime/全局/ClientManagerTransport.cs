using System;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class ClientManager
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
            Debug.Log("设置身份验证成功。");
            NetworkTime.ResetStatic();
            state = ConnectState.Connected;
            NetworkTime.Update();
            OnClientConnect?.Invoke();
            Ready();
        }

        /// <summary>
        /// 当客户端断开连接
        /// </summary>
        private static void OnClientDisconnected()
        {
            if (!isActive) return;
            Debug.Log("客户端断开传输。");
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
                Debug.LogError("没有有效的服务器连接！");
                return;
            }

            if (!readers.ReadEnqueue(data))
            {
                Debug.LogError($"无法将读取消息合批!");
                connection.Disconnect();
                return;
            }

            while (!isLoadScene && readers.ReadDequeue(out var reader, out double timestamp))
            {
                if (reader.Residue >= NetworkConst.EventSize)
                {
                    connection.timestamp = timestamp;
                    if (!TryInvoke(reader, channel))
                    {
                        Debug.LogWarning($"无法解包调用网络信息。");
                        connection.Disconnect();
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning($"网络消息应该有个开始的Id");
                    connection.Disconnect();
                    return;
                }
            }

            if (!isLoadScene && readers.Count > 0)
            {
                Debug.LogError($"读取器合批之后仍然还有次数残留！残留次数：{readers.Count}\n");
            }
        }

        /// <summary>
        /// 尝试读取并调用从服务器接收的委托
        /// </summary>
        /// <param name="reader">网络读取器</param>
        /// <param name="channel">传输通道</param>
        /// <returns>返回是否读取成功</returns>
        private static bool TryInvoke(NetworkReader reader, Channel channel)
        {
            if (NetworkEvent.ReadEvent(reader, out ushort id))
            {
                if (events.TryGetValue(id, out EventDelegate handle))
                {
                    handle.Invoke(connection, reader, channel);
                    return true;
                }

                Debug.LogWarning($"未知的网络消息Id：{id}");
                return false;
            }

            Debug.LogWarning("无效的网络消息类型！");
            return false;
        }
    }
}