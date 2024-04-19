using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace JFramework.Udp
{
    internal sealed partial class Proxy
    {
        /// <summary>
        /// 端对端状态
        /// </summary>
        private State state;

        /// <summary>
        /// 上一次发送Ping的时间
        /// </summary>
        private uint interval;

        /// <summary>
        /// 上一次接收消息的时间
        /// </summary>
        private uint received;

        /// <summary>
        /// 可靠Udp协议
        /// </summary>
        private readonly Protocol protocol;

        /// <summary>
        /// 缓存Id
        /// </summary>
        private readonly int cookie;

        /// <summary>
        /// 超时时间
        /// </summary>
        private readonly int timeout;

        /// <summary>
        /// 不可靠消息大小
        /// </summary>
        private readonly int unreliable;

        /// <summary>
        /// 直连缓冲区
        /// </summary>
        private readonly byte[] buffer;

        /// <summary>
        /// 可靠缓冲区
        /// </summary>
        private readonly byte[] fixedBuffer;

        /// <summary>
        /// 接收缓冲区
        /// </summary>
        private readonly byte[] receiveBuffer;

        /// <summary>
        /// 接收的缓存Id
        /// </summary>
        private readonly byte[] cookieBuffer = new byte[4];

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
        public Proxy(Setting setting, int cookie, Action OnAuthority, Action OnDisconnected, Action<ArraySegment<byte>> OnSend, Action<ArraySegment<byte>, Channel> OnReceive)
        {
            this.cookie = cookie;
            this.OnSend = OnSend;
            this.OnReceive = OnReceive;
            this.OnAuthority = OnAuthority;
            this.OnDisconnected = OnDisconnected;
            timeout = setting.timeout;
            protocol = new Protocol(0, SendReliable);
            protocol.SetUnit((uint)setting.maxUnit - Utility.METADATA_SIZE);
            protocol.SetResend(setting.interval, setting.resend);
            protocol.SetWindow(setting.sendSize, setting.receiveSize);
            unreliable = Utility.UnreliableSize(setting.maxUnit);
            receiveBuffer = new byte[Utility.ReliableSize(setting.maxUnit, setting.receiveSize) + 1];
            fixedBuffer = new byte[Utility.ReliableSize(setting.maxUnit, setting.receiveSize) + 1];
            buffer = new byte[setting.maxUnit];
            watch.Start();
        }

        /// <summary>
        /// 发送传输信息
        /// </summary>
        public void Send(ArraySegment<byte> segment, Channel channel)
        {
            if (segment.Count == 0)
            {
                Log.Error("Proxy尝试发送空消息。");
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
            if (fixedBuffer.Length < segment.Count + 1) // 减去消息头
            {
                Log.Error($"Proxy发送可靠消息失败。消息大小：{segment.Count}");
                return;
            }

            fixedBuffer[0] = (byte)header; // 设置缓冲区的头部
            if (segment.Count > 0)
            {
                Buffer.BlockCopy(segment.Array, segment.Offset, fixedBuffer, 1, segment.Count);
            }

            if (protocol.Send(fixedBuffer, 0, segment.Count + 1) < 0) // 加入到发送队列
            {
                Log.Error($"Proxy发送可靠消息失败。消息大小：{segment.Count}。");
            }
        }

        /// <summary>
        /// 根据长度的可靠传输
        /// </summary>
        private void SendReliable(byte[] message, int length)
        {
            buffer[0] = (byte)Channel.Reliable; // 消息通道
            Buffer.BlockCopy(cookieBuffer, 0, buffer, 1, 4); // 消息发送者
            Buffer.BlockCopy(message, 0, buffer, 1 + 4, length); // 消息内容
            OnSend?.Invoke(new ArraySegment<byte>(buffer, 0, length + 1 + 4));
        }

        /// <summary>
        /// 不可靠传输
        /// </summary>
        private void SendUnreliable(ArraySegment<byte> segment)
        {
            if (segment.Count > unreliable)
            {
                Log.Error($"Proxy发送不可靠消息失败。消息大小：{segment.Count}");
                return;
            }

            buffer[0] = (byte)Channel.Unreliable; // 消息通道
            Buffer.BlockCopy(cookieBuffer, 0, buffer, 1, 4);
            Buffer.BlockCopy(segment.Array, segment.Offset, buffer, 1 + 4, segment.Count);
            OnSend?.Invoke(new ArraySegment<byte>(buffer, 0, segment.Count + 1 + 4));
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
            var length = protocol.GetLength();
            if (length <= 0)
            {
                return false;
            }

            if (length > receiveBuffer.Length)
            {
                Log.Error($"Proxy消息长度不能超过{receiveBuffer.Length}。消息大小：{length}");
                Disconnect();
                return false;
            }

            if (protocol.Receive(receiveBuffer) < 0)
            {
                Log.Error($"Proxy接收消息失败。");
                Disconnect();
                return false;
            }

            header = (Header)receiveBuffer[0];
            segment = new ArraySegment<byte>(receiveBuffer, 1, length - 1);
            received = (uint)watch.ElapsedMilliseconds;
            return true;
        }

        /// <summary>
        /// 握手请求
        /// </summary>
        public void Handshake()
        {
            var cookieBytes = BitConverter.GetBytes(cookie);
            Log.Info($"Proxy发送握手请求。签名缓存：{cookie}");
            SendReliable(Header.Handshake, new ArraySegment<byte>(cookieBytes));
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
                protocol.Refresh();
            }
            catch
            {
                // ignored
            }

            Log.Info($"Proxy断开连接。");
            state = State.Disconnected;
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 当有消息被输入
        /// </summary>
        /// <param name="segment"></param>
        public void Input(ArraySegment<byte> segment)
        {
            if (segment.Count <= 1 + 4)
            {
                Log.Warn($"Proxy输入的消息缺少 消息头 或者 发送者");
                return;
            }

            var channel = (Channel)segment.Array[segment.Offset]; // 消息头
            var newCookie = BitConverter.ToUInt32(segment.Array, segment.Offset + 1); // 发送者

            if (state == State.Authority && newCookie != cookie)
            {
                Log.Warn($"Proxy丢弃了无效的签名缓存。旧：{cookie} 新：{newCookie}");
                return;
            }

            var message = new ArraySegment<byte>(segment.Array, segment.Offset + 1 + 4, segment.Count - 1 - 4);

            switch (channel)
            {
                case Channel.Reliable:
                    if (protocol.Input(message.Array, message.Offset, message.Count) != 0)
                    {
                        Log.Warn($"Proxy输入消息失败。消息大小：{message.Count - 1}");
                    }

                    break;
                case Channel.Unreliable:
                    if (state == State.Authority)
                    {
                        OnReceive?.Invoke(message, Channel.Unreliable);
                        received = (uint)watch.ElapsedMilliseconds;
                    }

                    break;
            }
        }
    }

    internal sealed partial class Proxy
    {
        public void EarlyUpdate()
        {
            uint time = (uint)watch.ElapsedMilliseconds;
            try
            {
                switch (state)
                {
                    case State.Connected:
                        EarlyUpdateConnected(time);
                        break;
                    case State.Authority:
                        EarlyUpdateAuthority(time);
                        break;
                }
            }
            catch (SocketException e)
            {
                Log.Error($"Proxy发生异常，断开连接。\n{e}.");
                Disconnect();
            }
            catch (ObjectDisposedException e)
            {
                Log.Error($"Proxy发生异常，断开连接。\n{e}.");
                Disconnect();
            }
            catch (Exception e)
            {
                Log.Error($"Proxy发生异常，断开连接。\n{e}.");
                Disconnect();
            }
        }

        public void AfterUpdate()
        {
            try
            {
                if (state == State.Connected || state == State.Authority)
                {
                    protocol.Update(watch.ElapsedMilliseconds);
                }
            }
            catch (SocketException e)
            {
                Log.Error($"Proxy发生异常，断开连接。\n{e}.");
                Disconnect();
            }
            catch (ObjectDisposedException e)
            {
                Log.Error($"Proxy发生异常，断开连接。\n{e}.");
                Disconnect();
            }
            catch (Exception e)
            {
                Log.Error($"Proxy发生异常，断开连接。\n{e}.");
                Disconnect();
            }
        }

        private void EarlyUpdateConnected(uint time)
        {
            OnEarlyUpdate(time);
            if (TryReceive(out var header, out var segment))
            {
                switch (header)
                {
                    case Header.Handshake:
                        if (segment.Count != 4)
                        {
                            Log.Error($"收到无效的握手消息。消息类型：{header}");
                            Disconnect();
                            return;
                        }

                        Buffer.BlockCopy(segment.Array, segment.Offset, cookieBuffer, 0, 4);
                        var prettyCookie = BitConverter.ToUInt32(segment.Array, segment.Offset);
                        Log.Info($"Proxy接收到握手消息。签名缓存：{prettyCookie}");
                        state = State.Authority;
                        OnAuthority?.Invoke();
                        break;
                    case Header.Disconnect:
                        Disconnect();
                        break;
                }
            }
        }

        private void EarlyUpdateAuthority(uint time)
        {
            OnEarlyUpdate(time);
            while (TryReceive(out var header, out var segment))
            {
                switch (header)
                {
                    case Header.Handshake:
                        Log.Warn($"Proxy身份验证时收到无效的消息。消息类型：{header}");
                        Disconnect();
                        break;
                    case Header.Message:
                        if (segment.Count > 0)
                        {
                            OnReceive?.Invoke(segment, Channel.Reliable);
                        }
                        else
                        {
                            Log.Error("Proxy通过身份验证时收到空数据消息。");
                            Disconnect();
                        }

                        break;
                    case Header.Disconnect:
                        Log.Info($"Proxy接收到断开连接的消息。");
                        Disconnect();
                        break;
                }
            }
        }

        private void OnEarlyUpdate(uint time)
        {
            if (time >= received + timeout)
            {
                Log.Error($"Proxy在 {timeout}ms 内没有收到任何消息后的连接超时！");
                Disconnect();
            }

            if (protocol.state == -1)
            {
                Log.Error($"Proxy消息被重传了 {Protocol.DEAD_LINK} 次而没有得到确认！");
                Disconnect();
            }

            if (time >= interval + Utility.PING_INTERVAL)
            {
                SendReliable(Header.Ping, default);
                interval = time;
            }
            
            if (!protocol.IsQuickly())
            {
                Log.Error("Proxy断开连接，因为它处理数据的速度不够快！");
                Disconnect();
            }
        }
    }
}