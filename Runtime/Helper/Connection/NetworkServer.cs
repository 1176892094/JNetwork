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
        private Dictionary<int, WriterPool> writerPools = new Dictionary<int, WriterPool>();
        internal List<NetworkWriter> writers = new List<NetworkWriter>();
        [SerializeField] internal ReaderPool readerPool = new ReaderPool();
        [SerializeField] internal bool isReady;
        [SerializeField] internal double remoteTime;
        
        internal void Update()
        {
            foreach (var (channel, writerPool) in writerPools) // 遍历可靠和不可靠消息
            {
                using var writer = NetworkWriter.Pop(); // 取出 writer
                while (writerPool.TryWrite(writer)) // 将数据拷贝到 writer
                {
                    ArraySegment<byte> segment = writer; // 将 writer 转化成数据分段
                    if (NetworkUtility.IsValid(segment, channel)) // 判断 writer 是否有效
                    {
                        NetworkManager.Transport.SendToServer(segment, channel); // 发送数据到传输层
                    }
                }
            }

            if (NetworkManager.Mode == EntryMode.Host)
            {
                while (writers.Count > 0)
                {
                    using var writer = writers[0];
                    writers.RemoveAt(0);
                    if (writerPools.TryGetValue(Channel.Reliable, out var writerPool))
                    {
                        writerPool.Write(writer, NetworkManager.TickTime);
                        using var target = NetworkWriter.Pop();
                        if (writerPool.TryWrite(target))
                        {
                            NetworkManager.Client.OnClientReceive(target, Channel.Reliable);
                        }
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
            if (!writerPools.TryGetValue(channel, out var writerPool))
            {
                writerPool = new WriterPool(channel);
                writerPools[channel] = writerPool;
            }

            if (NetworkManager.Mode != EntryMode.Host)
            {
                writerPool.Write(segment, NetworkManager.TickTime);
                return;
            }

            if (segment.Count == 0)
            {
                Debug.LogError("发送消息大小不能为零！");
                return;
            }

            writerPool.Write(segment, NetworkManager.TickTime);
            using var writer = NetworkWriter.Pop();
            if (!writerPool.TryWrite(writer))
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