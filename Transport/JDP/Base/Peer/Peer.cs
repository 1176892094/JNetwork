using System;
using System.Diagnostics;

// ReSharper disable All

namespace JFramework.Udp
{
    internal sealed partial class Peer
    {
        /// <summary>
        /// 端对端状态
        /// </summary>
        private State state;

        /// <summary>
        /// 上一次发送Ping的时间
        /// </summary>
        private uint lastPingTime;

        /// <summary>
        /// 上一次接收消息的时间
        /// </summary>
        private uint lastReceiveTime;

        /// <summary>
        /// 可靠Udp协议
        /// </summary>
        private readonly Jdp jdp;

        /// <summary>
        /// 缓存Id
        /// </summary>
        private readonly int cookie;

        /// <summary>
        /// 超时时间
        /// </summary>
        private readonly int timeout;

        /// <summary>
        /// 可靠消息大小
        /// </summary>
        private readonly int reliableSize;

        /// <summary>
        /// 不可靠消息大小
        /// </summary>
        private readonly int unreliableSize;

        /// <summary>
        /// 消息缓冲区
        /// </summary>
        private readonly byte[] messageBuffer;

        /// <summary>
        /// 发送协议缓冲区
        /// </summary>
        private readonly byte[] jdpSendBuffer;

        /// <summary>
        /// 低等级发送缓存区
        /// </summary>
        private readonly byte[] rawSendBuffer;

        /// <summary>
        /// 接收的缓存Id
        /// </summary>
        private readonly byte[] receiveCookie = new byte[4];

        /// <summary>
        /// 计时器
        /// </summary>
        private readonly Stopwatch watch = new Stopwatch();

        /// <summary>
        /// 当客户端通过验证
        /// </summary>
        private event Action OnAuthority;

        /// <summary>
        /// 当客户端或服务器断开连接
        /// </summary>
        private event Action OnDisconnected;

        /// <summary>
        /// 当服务器或客户端发送消息
        /// </summary>
        private event Action<ArraySegment<byte>> OnSend;

        /// <summary>
        /// 当服务器或客户端接收消息
        /// </summary>
        private event Action<ArraySegment<byte>, Channel> OnReceive;

        /// <summary>
        /// 构造函数初始化
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="cookie"></param>
        /// <param name="OnAuthority"></param>
        /// <param name="OnDisconnected"></param>
        /// <param name="OnSend"></param>
        /// <param name="OnReceive"></param>
        public Peer(Setting setting, int cookie, Action OnAuthority, Action OnDisconnected, Action<ArraySegment<byte>> OnSend, Action<ArraySegment<byte>, Channel> OnReceive)
        {
            this.cookie = cookie;
            this.OnSend = OnSend;
            this.OnReceive = OnReceive;
            this.OnAuthority = OnAuthority;
            this.OnDisconnected = OnDisconnected;
            timeout = setting.timeout;
            jdp = new Jdp(0, SendReliable);
            jdp.SetNoDelay(setting.noDelay ? 1U : 0U, setting.interval, setting.resend, setting.congestion);
            jdp.SetWindowSize(setting.sendPacketSize, setting.receivePacketSize);
            jdp.SetTransferUnit((uint)setting.maxTransferUnit - Helper.METADATA_SIZE);
            reliableSize = Helper.ReliableSize(setting.maxTransferUnit, setting.receivePacketSize);
            unreliableSize = Helper.UnreliableSize(setting.maxTransferUnit);
            messageBuffer = new byte[reliableSize + 1];
            jdpSendBuffer = new byte[reliableSize + 1];
            rawSendBuffer = new byte[setting.maxTransferUnit];
            watch.Start();
        }

        /// <summary>
        /// 发送传输信息
        /// </summary>
        public void Send(ArraySegment<byte> segment, Channel channel)
        {
            if (segment.Count == 0)
            {
                Log.Error("P2P尝试发送空消息。");
                Disconnect();
                return;
            }

            switch (channel)
            {
                case Channel.Reliable:
                    SendReliable(Header.Message, segment);
                    break;
                case Channel.Unreliable:
                    SendUnreliable(segment);
                    break;
            }
        }

        /// <summary>
        /// 根据消息头的可靠传输
        /// </summary>
        /// <param name="header"></param>
        /// <param name="segment"></param>
        private void SendReliable(Header header, ArraySegment<byte> segment)
        {
            if (segment.Count > jdpSendBuffer.Length - 1)
            {
                Log.Error($"P2P发送可靠消息失败。消息大小：{segment.Count}");
                return;
            }

            jdpSendBuffer[0] = (byte)header; //设置传输的头部

            if (segment.Count > 0)
            {
                Buffer.BlockCopy(segment.Array, segment.Offset, jdpSendBuffer, 1, segment.Count);
            }

            int sent = jdp.Send(jdpSendBuffer, 0, segment.Count + 1);
            if (sent < 0)
            {
                Log.Error($"P2P发送可靠消息失败。消息大小：{segment.Count}。消息类型：{sent}");
            }
        }

        /// <summary>
        /// 根据长度的可靠传输
        /// </summary>
        private void SendReliable(byte[] message, int length)
        {
            rawSendBuffer[0] = (byte)Channel.Reliable;
            Buffer.BlockCopy(receiveCookie, 0, rawSendBuffer, 1, 4);
            Buffer.BlockCopy(message, 0, rawSendBuffer, 1 + 4, length);
            var segment = new ArraySegment<byte>(rawSendBuffer, 0, length + 1 + 4);
            OnSend.Invoke(segment);
        }

        /// <summary>
        /// 不可靠传输
        /// </summary>
        private void SendUnreliable(ArraySegment<byte> segment)
        {
            if (segment.Count > unreliableSize)
            {
                Log.Error($"P2P发送不可靠消息失败。消息大小：{segment.Count}");
                return;
            }

            rawSendBuffer[0] = (byte)Channel.Unreliable;
            Buffer.BlockCopy(receiveCookie, 0, rawSendBuffer, 1, 4);
            Buffer.BlockCopy(segment.Array, segment.Offset, rawSendBuffer, 1 + 4, segment.Count);
            var message = new ArraySegment<byte>(rawSendBuffer, 0, segment.Count + 1 + 4);
            OnSend.Invoke(message);
        }

        /// <summary>
        /// 尝试接收消息
        /// </summary>
        /// <param name="header">消息的头部</param>
        /// <param name="segment">数据分段</param>
        /// <returns>返回是否能接收</returns>
        private bool TryReceive(out Header header, out ArraySegment<byte> segment)
        {
            segment = default;
            header = Header.Disconnect;
            int messageSize = jdp.PeekSize();
            if (messageSize <= 0)
            {
                return false;
            }

            if (messageSize > messageBuffer.Length)
            {
                Log.Error($"P2P消息长度不能超过{messageBuffer.Length}。消息大小：{messageSize}");
                Disconnect();
                return false;
            }

            int received = jdp.Receive(messageBuffer, messageSize);
            if (received < 0)
            {
                Log.Error($"P2P接收消息失败。消息类型：{received}");
                Disconnect();
                return false;
            }

            header = (Header)messageBuffer[0];
            segment = new ArraySegment<byte>(messageBuffer, 1, messageSize - 1);
            lastReceiveTime = (uint)watch.ElapsedMilliseconds;
            return true;
        }

        /// <summary>
        /// 握手请求
        /// </summary>
        public void Handshake()
        {
            var cookieBytes = BitConverter.GetBytes(cookie);
            Log.Info($"P2P发送握手请求。签名缓存：{cookie}");
            var segment = new ArraySegment<byte>(cookieBytes);
            SendReliable(Header.Handshake, segment);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (state == State.Disconnected) return;
            try
            {
                SendReliable(Header.Disconnect, default);
                jdp.Flush();
            }
            catch
            {
                // ignored
            }

            Log.Info($"P2P断开连接。");
            state = State.Disconnected;
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 当有消息被输入
        /// </summary>
        /// <param name="segment"></param>
        public void Input(ArraySegment<byte> segment)
        {
            if (segment.Count <= 5) return;
            var channel = segment.Array[segment.Offset];
            var newCookie = BitConverter.ToUInt32(segment.Array, segment.Offset + 1);

            if (state == State.Authority && newCookie != cookie)
            {
                Log.Warn($"P2P丢弃了无效的签名缓存。旧：{cookie} 新：{newCookie}");
                return;
            }

            var message = new ArraySegment<byte>(segment.Array, segment.Offset + 1 + 4, segment.Count - 1 - 4);

            switch (channel)
            {
                case (byte)Channel.Reliable:
                    int input = jdp.Input(message.Array, message.Offset, message.Count);
                    if (input != 0)
                    {
                        Log.Warn($"P2P输入消息失败。错误代码：{input}。消息大小：{message.Count - 1}");
                    }

                    break;
                case (byte)Channel.Unreliable:
                    if (state == State.Authority)
                    {
                        OnReceive?.Invoke(message, Channel.Unreliable);
                        lastReceiveTime = (uint)watch.ElapsedMilliseconds;
                    }

                    break;
            }
        }
    }
}