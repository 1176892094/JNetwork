using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JFramework.Interface;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 网络连接 (Server or Client)
    /// </summary>
    public abstract class Connection
    {
        /// <summary>
        /// 存储不同传输通道写入的网络信息
        /// </summary>
        private readonly Dictionary<Channel, NetworkWriterPack> writerPacks = new Dictionary<Channel, NetworkWriterPack>();

        /// <summary>
        /// 是否准备好可以接收信息
        /// </summary>
        [ShowInInspector] internal bool isReady;

        /// <summary>
        /// 远端时间戳
        /// </summary>
        internal double timestamp;

        /// <summary>
        /// 网络消息更新
        /// </summary>
        internal virtual void Update()
        {
            foreach (var (channel, writerPack) in writerPacks) // 遍历可靠和不可靠消息
            {
                using var writer = NetworkWriter.Pop(); // 取出 writer
                while (writerPack.WriteDequeue(writer)) // 将数据拷贝到 writer
                {
                    var segment = writer.ToArraySegment(); // 将 writer 转化成数据分段
                    if (IsValid(segment, channel)) // 判断是否 writer 是否有效
                    {
                        SendToTransport(segment, channel); // 发送数据到传输层
                        writer.position = 0; // 重置 writer 的位置
                    }
                }
            }
        }

        /// <summary>
        /// 约束范围在0到最大之间，否侧报错
        /// </summary>
        /// <param name="segment">数据分段</param>
        /// <param name="channel">传输通道</param>
        /// <returns>返回是否有效</returns>
        private static bool IsValid(ArraySegment<byte> segment, Channel channel)
        {
            int maxPacketSize = Transport.current.GetMaxPacketSize(channel);
            if (segment.Count > maxPacketSize)
            {
                Debug.LogError($"发送消息大小不能超过{maxPacketSize}。消息大小：{segment.Count}");
                return false;
            }

            if (segment.Count == 0)
            {
                Debug.LogError("发送消息大小不能为零！");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 发送网络消息
        /// </summary>
        /// <param name="event">事件类型</param>
        /// <param name="channel">传输通道</param>
        /// <typeparam name="T">传入NetworkMessage</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send<T>(T @event, Channel channel = Channel.Reliable) where T : struct, IEvent
        {
            using var writer = NetworkWriter.Pop();
            NetworkEvent.WriteEvent(writer, @event);
            Send(writer.ToArraySegment(), channel);
        }
        
        /// <summary>
        /// 网络消息发送
        /// </summary>
        /// <param name="channel"></param>
        /// <returns>返回一个发送类</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected NetworkWriterPack GetWriters(Channel channel)
        {
            if (writerPacks.TryGetValue(channel, out var writers)) return writers;
            var threshold = Transport.current.UnreliableSize();
            return writerPacks[channel] = new NetworkWriterPack(threshold);
        }

        /// <summary>
        /// 获取网络消息并添加到发送队列中
        /// </summary>
        /// <param name="segment">数据分段</param>
        /// <param name="channel">传输通道</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal abstract void Send(ArraySegment<byte> segment, Channel channel = Channel.Reliable);

        /// <summary>
        /// 发送消息到传输层
        /// </summary>
        /// <param name="segment">数据分段</param>
        /// <param name="channel">传输通道</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable);
        
        /// <summary>
        /// 连接断开
        /// </summary>
        public abstract void Disconnect();
    }
}