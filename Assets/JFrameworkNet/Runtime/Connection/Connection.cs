using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public abstract class Connection
    {
        private readonly Dictionary<Channel, NetworkWriters> sends = new Dictionary<Channel, NetworkWriters>();
        public readonly HashSet<NetworkObject> objects = new HashSet<NetworkObject>();
        public bool isReady;
        public bool isLocal;
        public bool isAuthority;
        public double timestamp;

        /// <summary>
        /// 网络消息更新
        /// </summary>
        internal virtual void Update()
        {
            foreach (var (channel, send) in sends) // 遍历可靠和不可靠消息
            {
                using var writer = NetworkWriter.Pop();
                while (send.WriteDequeue(writer))
                {
                    var segment = writer.ToArraySegment();
                    if (IsValid(segment, channel))
                    {
                        SendToTransport(segment, channel);
                        writer.position = 0;
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
                Debug.LogError($"Cannot send packet too large: {maxPacketSize} > {segment.Count}");
                return false;
            }

            if (segment.Count == 0)
            {
                Debug.LogError("Cannot send 0 bytes");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 发送网络消息
        /// </summary>
        /// <param name="message">数据分段</param>
        /// <param name="channel">传输通道</param>
        /// <typeparam name="T">传入NetworkMessage</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send<T>(T message, Channel channel = Channel.Reliable) where T : struct, IEvent
        {
            using var writer = NetworkWriter.Pop();
            NetworkUtils.WriteMessage(writer,message);
            Send(writer.ToArraySegment(), channel);
        }

        /// <summary>
        /// 获取网络消息并添加到发送队列中
        /// </summary>
        /// <param name="segment">数据分段</param>
        /// <param name="channel">传输通道</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Send(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            GetWriters(channel).WriteEnqueue(segment, NetworkTime.localTime);
        }
        
        /// <summary>
        /// 发送消息到传输层
        /// </summary>
        /// <param name="segment">数据分段</param>
        /// <param name="channel">传输通道</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable);
        
        /// <summary>
        /// 网络消息发送
        /// </summary>
        /// <param name="channel"></param>
        /// <returns>返回一个发送类</returns>
        protected NetworkWriters GetWriters(Channel channel)
        {
            if (sends.TryGetValue(channel, out var send)) return send;
            Debug.Log($"Connection --> GetWriters : {GetType()}");
            var size = Transport.current.UnreliableSize();
            return sends[channel] = new NetworkWriters(size);
        }
        
        /// <summary>
        /// 连接断开
        /// </summary>
        public abstract void Disconnect();
    }
}