// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  14:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    [Serializable]
    public class NetworkClient
    {
        private Dictionary<int, WriterPool> writerPools = new Dictionary<int, WriterPool>();
        private Dictionary<int, NetworkWriter> writers = new Dictionary<int, NetworkWriter>();
        [SerializeField] internal ReaderPool readerPool = new ReaderPool();
        [SerializeField] internal int clientId;
        [SerializeField] internal bool isReady;
        [SerializeField] internal bool isPlayer;
        [SerializeField] internal double remoteTime;

        public NetworkClient(int clientId)
        {
            this.clientId = clientId;
            writers.Add(Channel.Reliable, new NetworkWriter());
            writers.Add(Channel.Unreliable, new NetworkWriter());
        }

        internal void Update()
        {
            foreach (var (channel, writer) in writers)
            {
                if (writer.position > 0)
                {
                    Send(new InvokeMessage(writer), channel);
                    writer.position = 0;
                }
            }

            foreach (var (channel, writerPool) in writerPools) // 遍历可靠和不可靠消息
            {
                using var writer = NetworkWriter.Pop(); // 取出 writer
                while (writerPool.TryWrite(writer)) // 将数据拷贝到 writer
                {
                    ArraySegment<byte> segment = writer; // 将 writer 转化成数据分段
                    if (NetworkUtility.IsValid(segment, channel)) // 判断 writer 是否有效
                    {
                        NetworkManager.Transport.SendToClient(clientId, segment, channel);
                        writer.position = 0;
                    }
                }
            }
        }

        /// <summary>
        /// 发送网络消息
        /// </summary>
        /// <param name="message">事件类型</param>
        /// <param name="channel">传输通道</param>
        /// <typeparam name="T">传入NetworkMessage</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send<T>(T message, int channel = Channel.Reliable) where T : struct, Message
        {
            using var writer = NetworkWriter.Pop();
            writer.WriteUShort(Message<T>.Id);
            writer.Invoke(message);
            Send(writer, channel);
        }

        /// <summary>
        /// 获取网络消息并添加到发送队列中
        /// </summary>
        /// <param name="segment">数据分段</param>
        /// <param name="channel">传输通道</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Send(ArraySegment<byte> segment, int channel = Channel.Reliable)
        {
            if (clientId == Const.HostId)
            {
                var writer = NetworkWriter.Pop();
                writer.WriteBytes(segment.Array, segment.Offset, segment.Count);
                NetworkManager.Client.connection.writers.Add(writer);
                return;
            }

            if (!writerPools.TryGetValue(channel, out var writerPool))
            {
                writerPool = new WriterPool(channel);
                writerPools[channel] = writerPool;
            }

            writerPool.Write(segment, NetworkManager.TickTime);
        }

        /// <summary>
        /// 由NetworkBehaviour调用
        /// </summary>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        internal void Send(ClientRpcMessage message, int channel)
        {
            if (writers.TryGetValue(channel, out var writer))
            {
                var maxSize = NetworkManager.Transport.MessageSize(channel);
                int size = maxSize - Const.MessageSize - sizeof(int) - Const.HeaderSize;
                int position = writer.position;
                writer.Invoke(message);
                int messageSize = writer.position - position;
                if (messageSize > size)
                {
                    Debug.LogWarning($"远程调用 {message.objectId} 消息大小不能超过 {size}。消息大小：{messageSize}");
                    return;
                }

                if (writer.position > size)
                {
                    writer.position = position;
                    if (writer.position > 0)
                    {
                        Send(new InvokeMessage(writer), channel);
                        writer.position = 0;
                    }

                    writer.Invoke(message);
                }
            }
        }

        /// <summary>
        /// 是否准备好可以接收信息
        /// </summary>
        public void Disconnect()
        {
            isReady = false;
            NetworkManager.Transport.StopClient(clientId);
        }
    }
}