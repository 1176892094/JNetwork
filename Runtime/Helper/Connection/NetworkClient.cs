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
        private Dictionary<int, WriterBatch> writerBatches = new Dictionary<int, WriterBatch>();
        private Dictionary<int, NetworkWriter> writers = new Dictionary<int, NetworkWriter>();
        [SerializeField] internal ReaderBatch readerBatch = new ReaderBatch();
        [SerializeField] internal int clientId;
        [SerializeField] internal bool isReady;
        [SerializeField] internal bool isPlayer;
        [SerializeField] internal double remoteTime;

        public NetworkClient(int clientId)
        {
            this.clientId = clientId;
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

            foreach (var (channel, writerBatch) in writerBatches)
            {
                using var writer = NetworkWriter.Pop();
                while (writerBatch.GetBatch(writer))
                {
                    ArraySegment<byte> segment = writer;
                    NetworkManager.Transport.SendToClient(clientId, segment, channel);
                    writer.position = 0;
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
            if (writer.position > NetworkManager.Transport.MessageSize(channel))
            {
                Debug.LogError($"发送消息大小过大！消息大小：{writer.position}");
                return;
            }

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
                NetworkManager.Client.connection.writers.Enqueue(writer);
                return;
            }

            if (!writerBatches.TryGetValue(channel, out var writerBatch))
            {
                writerBatch = new WriterBatch(channel);
                writerBatches[channel] = writerBatch;
            }

            writerBatch.AddMessage(segment, NetworkManager.TickTime);
        }

        /// <summary>
        /// 由NetworkBehaviour调用
        /// </summary>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        internal void Send(ClientRpcMessage message, int channel)
        {
            if (!writers.TryGetValue(channel, out var writer))
            {
                writer = new NetworkWriter();
                writers[channel] = writer;
            }

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