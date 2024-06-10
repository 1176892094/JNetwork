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
        private event Action OnConnect;

        /// <summary>
        /// 当客户端或服务器断开连接
        /// </summary>
        private event Action OnDisconnect;

        /// <summary>
        /// 当服务器或客户端发送消息
        /// </summary>
        private event Action<ArraySegment<byte>> OnSend;

        /// <summary>
        /// 当服务器或客户端接收消息
        /// </summary>
        private event Action<ArraySegment<byte>, int> OnReceive;

        /// <summary>
        /// 构造函数初始化
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="cookie"></param>
        /// <param name="OnConnect"></param>
        /// <param name="OnDisconnect"></param>
        /// <param name="OnSend"></param>
        /// <param name="OnReceive"></param>
        public Proxy(Setting setting, int cookie, Action OnConnect, Action OnDisconnect, Action<ArraySegment<byte>> OnSend, Action<ArraySegment<byte>, int> OnReceive)
        {
            this.cookie = cookie;
            this.OnSend = OnSend;
            this.OnReceive = OnReceive;
            this.OnConnect = OnConnect;
            this.OnDisconnect = OnDisconnect;
            timeout = setting.timeout;
            protocol = new Protocol(0, SendReliable);
            protocol.SetUnit((uint)setting.unit - Utility.METADATA_SIZE);
            protocol.SetResend(setting.interval, setting.resend);
            protocol.SetWindow(setting.send, setting.receive);
            unreliable = Utility.UnreliableSize(setting.unit);
            receiveBuffer = new byte[Utility.ReliableSize(setting.unit, setting.receive) + 1];
            fixedBuffer = new byte[Utility.ReliableSize(setting.unit, setting.receive) + 1];
            buffer = new byte[setting.unit];
            watch.Start();
        }

        /// <summary>
        /// 发送传输信息
        /// </summary>
        public void Send(ArraySegment<byte> segment, int channel)
        {
            if (segment.Count == 0)
            {
                Log.Error("网络代理尝试发送空消息。");
                Disconnect();
                return;
            }

            switch (channel)
            {
                case 1:
                    SendReliable(Head.Data, segment);
                    break;
                case 2:
                    SendUnreliable(segment);
                    break;
            }
        }

        /// <summary>
        /// 根据消息头的可靠传输
        /// </summary>
        /// <param name="header"></param>
        /// <param name="segment"></param>
        private void SendReliable(Head header, ArraySegment<byte> segment)
        {
            if (fixedBuffer.Length < segment.Count + 1) // 减去消息头
            {
                Log.Error($"网络代理发送可靠消息失败。消息大小：{segment.Count}");
                return;
            }

            fixedBuffer[0] = (byte)header; // 设置缓冲区的头部
            if (segment.Count > 0)
            {
                Buffer.BlockCopy(segment.Array, segment.Offset, fixedBuffer, 1, segment.Count);
            }

            if (protocol.Send(fixedBuffer, 0, segment.Count + 1) < 0) // 加入到发送队列
            {
                Log.Error($"网络代理发送可靠消息失败。消息大小：{segment.Count}。");
            }
        }

        /// <summary>
        /// 根据长度的可靠传输
        /// </summary>
        private void SendReliable(byte[] message, int length)
        {
            buffer[0] = 1; // 消息通道
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
                Log.Error($"网络代理发送不可靠消息失败。消息大小：{segment.Count}");
                return;
            }

            buffer[0] = 2; // 消息通道
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
        private bool TryReceive(out Head header, out ArraySegment<byte> segment)
        {
            segment = default;
            header = Head.Disconnect;
            var length = protocol.GetLength();
            if (length <= 0)
            {
                return false;
            }

            if (length > receiveBuffer.Length)
            {
                Log.Error($"网络消息长度不能超过{receiveBuffer.Length}。消息大小：{length}");
                Disconnect();
                return false;
            }

            if (protocol.Receive(receiveBuffer) < 0)
            {
                Log.Error($"网络代理接收消息失败。");
                Disconnect();
                return false;
            }

            header = (Head)receiveBuffer[0];
            segment = new ArraySegment<byte>(receiveBuffer, 1, length - 1);
            received = (uint)watch.ElapsedMilliseconds;
            return true;
        }

        /// <summary>
        /// 连接请求
        /// </summary>
        public void Connect()
        {
            var segment = new ArraySegment<byte>(BitConverter.GetBytes(cookie));
            SendReliable(Head.Connect, segment);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (state == State.Disconnect) return;
            try
            {
                SendReliable(Head.Disconnect, default);
                protocol.Refresh();
            }
            catch
            {
                // ignored
            }

            state = State.Disconnect;
            OnDisconnect?.Invoke();
        }

        /// <summary>
        /// 当有消息被输入
        /// </summary>
        /// <param name="segment"></param>
        public void Input(ArraySegment<byte> segment)
        {
            if (segment.Count <= 1 + 4)
            {
                Log.Info("网络代理发送的消息过短。");
                return;
            }

            var channel = segment.Array[segment.Offset]; // 消息头
            var newCookie = BitConverter.ToUInt32(segment.Array, segment.Offset + 1); // 发送者

            if (state == State.Connected && newCookie != cookie)
            {
                Log.Info($"网络代理丢弃了无效的签名缓存。旧：{cookie} 新：{newCookie}");
                return;
            }

            var message = new ArraySegment<byte>(segment.Array, segment.Offset + 1 + 4, segment.Count - 1 - 4);

            switch (channel)
            {
                case 1:
                    if (protocol.Input(message.Array, message.Offset, message.Count) != 0)
                    {
                        Log.Warn($"网络代理发送可靠消息失败。消息大小：{message.Count - 1}");
                    }

                    break;
                case 2:
                    if (state == State.Connected)
                    {
                        OnReceive?.Invoke(message, 2);
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
            try
            {
                if (state == State.Disconnect) return;
                if (protocol.state == -1)
                {
                    Log.Error($"网络消息被重传了 {Protocol.DEAD} 次而没有得到确认！");
                    Disconnect();
                    return;
                }

                if (!protocol.IsFaster())
                {
                    Log.Error("网络代理断开连接，因为它处理数据的速度不够快！");
                    Disconnect();
                    return;
                }

                var seconds = (uint)watch.ElapsedMilliseconds;
                if (seconds >= received + timeout)
                {
                    Log.Error($"网络代理在 {timeout}ms 内没有收到任何消息后的连接超时！");
                    Disconnect();
                    return;
                }

                if (seconds >= interval + Utility.PING_INTERVAL)
                {
                    SendReliable(Head.Ping, default);
                    interval = seconds;
                }

                if (TryReceive(out var header, out var segment))
                {
                    if (header == Head.Disconnect)
                    {
                        Disconnect();
                        return;
                    }

                    if (state == State.Connect)
                    {
                        switch (header)
                        {
                            case Head.Connect when segment.Count != 4:
                                Log.Error($"收到无效的握手消息。消息类型：{header}");
                                Disconnect();
                                break;
                            case Head.Connect:
                                Buffer.BlockCopy(segment.Array, segment.Offset, cookieBuffer, 0, 4);
                                state = State.Connected;
                                OnConnect?.Invoke();
                                break;
                        }
                    }
                    else
                    {
                        switch (header)
                        {
                            case Head.Connect:
                                Log.Error($"收到无效的握手消息。消息类型：{header}");
                                Disconnect();
                                break;
                            case Head.Data when segment.Count <= 0:
                                Log.Error($"收到无效的握手消息。消息类型：{header}");
                                Disconnect();
                                break;
                            case Head.Data:
                                OnReceive?.Invoke(segment, 1);
                                break;
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                Log.Error($"网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
            catch (ObjectDisposedException e)
            {
                Log.Error($"网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
            catch (Exception e)
            {
                Log.Error($"网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
        }


        public void AfterUpdate()
        {
            try
            {
                if (state == State.Disconnect) return;
                protocol.Update(watch.ElapsedMilliseconds);
            }
            catch (SocketException e)
            {
                Log.Error($"网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
            catch (ObjectDisposedException e)
            {
                Log.Error($"网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
            catch (Exception e)
            {
                Log.Error($"网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
        }
    }
}