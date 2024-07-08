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
    public class NetworkServer
    {
        private Dictionary<int, WriterBatch> writerBatches = new Dictionary<int, WriterBatch>();
        internal Queue<NetworkWriter> writers = new Queue<NetworkWriter>();
        [SerializeField] internal ReaderBatch readerBatch = new ReaderBatch();
        [SerializeField] internal bool isReady;
        [SerializeField] internal double remoteTime;

        internal void Update()
        {
            foreach (var (channel, writerBatch) in writerBatches)
            {
                using var writer = NetworkWriter.Pop();
                while (writerBatch.GetBatch(writer))
                {
                    ArraySegment<byte> segment = writer;
                    NetworkManager.Transport.SendToServer(segment, channel);
                    writer.position = 0;
                }
            }

            while (writers.Count > 0)
            {
                using var writer = writers.Dequeue();
                if (writerBatches.TryGetValue(Channel.Reliable, out var writerBatch))
                {
                    writerBatch.AddMessage(writer, NetworkManager.TickTime);
                    using var target = NetworkWriter.Pop();
                    if (writerBatch.GetBatch(target))
                    {
                        NetworkManager.Client.OnClientReceive(target, Channel.Reliable);
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
            if (!writerBatches.TryGetValue(channel, out var writerBatch))
            {
                writerBatch = new WriterBatch(channel);
                writerBatches[channel] = writerBatch;
            }

            if (NetworkManager.Mode != EntryMode.Host)
            {
                writerBatch.AddMessage(segment, NetworkManager.TickTime);
                return;
            }

            if (segment.Count == 0)
            {
                Debug.LogError("发送消息大小不能为零！");
                return;
            }

            writerBatch.AddMessage(segment, NetworkManager.TickTime);
            using var writer = NetworkWriter.Pop();
            if (!writerBatch.GetBatch(writer))
            {
                Debug.LogError("无法拷贝数据到写入器。");
                return;
            }

            NetworkManager.Server.OnServerReceive(Const.HostId, writer, channel);
        }

        public void Disconnect()
        {
            isReady = false;
            NetworkManager.Client.isReady = false;
            NetworkManager.Transport.StopClient();
        }
    }
}