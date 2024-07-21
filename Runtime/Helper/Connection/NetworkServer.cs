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
        private Dictionary<byte, WriterBatch> writerBatches = new Dictionary<byte, WriterBatch>();
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
                    NetworkManager.Transport.SendToServer(writer, channel);
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
        public void Send<T>(T message, byte channel = Channel.Reliable) where T : struct, Message
        {
            using var writer = NetworkWriter.Pop();
            writer.WriteUShort(Message<T>.Id);
            writer.Invoke(message);
            if (writer.position > NetworkManager.Transport.MessageSize(channel))
            {
                Debug.LogError($"发送消息大小过大！消息大小：{writer.position}");
                return;
            }

            if (!writerBatches.TryGetValue(channel, out var writerBatch))
            {
                writerBatch = new WriterBatch(channel);
                writerBatches[channel] = writerBatch;
            }

            writerBatch.AddMessage(writer, NetworkManager.TickTime);
            if (NetworkManager.Mode == EntryMode.Host)
            {
                using var target = NetworkWriter.Pop();
                if (writerBatch.GetBatch(target))
                {
                    NetworkManager.Server.OnServerReceive(Const.HostId, target, Channel.Reliable);
                }
            }
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