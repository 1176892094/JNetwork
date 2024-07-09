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
        [SerializeField] internal ReaderBatch reader = new ReaderBatch();
        [SerializeField] internal bool isReady;
        [SerializeField] internal double remoteTime;

        /// <summary>
        /// 将消息发送到传输层
        /// </summary>
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
                using var target = writers.Dequeue();
                using var writer = NetworkWriter.Pop();
                if (AddMessage(target).GetBatch(writer))
                {
                    NetworkManager.Client.OnClientReceive(writer, Channel.Reliable);
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

            if (NetworkManager.Mode != EntryMode.Host)
            {
                AddMessage(writer, channel);
                return;
            }

            using var target = NetworkWriter.Pop();
            if (AddMessage(writer).GetBatch(target))
            {
                NetworkManager.Server.OnServerReceive(Const.HostId, target, channel);
            }
        }

        /// <summary>
        /// 网络消息添加
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="channel"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal WriterBatch AddMessage(ArraySegment<byte> segment, int channel = Channel.Reliable)
        {
            if (!writerBatches.TryGetValue(channel, out var writerBatch))
            {
                writerBatch = new WriterBatch(channel);
                writerBatches[channel] = writerBatch;
            }

            writerBatch.AddMessage(segment, NetworkManager.TickTime);
            return writerBatch;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            isReady = false;
            NetworkManager.Client.isReady = false;
            NetworkManager.Transport.StopClient();
        }
    }
}