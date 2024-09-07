using System;
using System.Diagnostics;
using System.Net.Sockets;

// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace JFramework.Udp
{
    public abstract class Proxy
    {
        protected uint cookie;
        protected State state;
        private Kcp kcp;
        private uint timeout;
        private uint pingTime;
        private uint receiveTime;
        private readonly int unreliableSize;
        private readonly byte[] kcpSendBuffer;
        private readonly byte[] rawSendBuffer;
        private readonly byte[] receiveBuffer;
        private readonly Stopwatch watch = new Stopwatch();

        protected Proxy(Setting setting, uint cookie)
        {
            Reset(setting);
            this.cookie = cookie;
            unreliableSize = Common.UnreliableSize(setting.MaxUnit);
            var reliableSize = Common.ReliableSize(setting.MaxUnit, setting.ReceiveWindow);
            rawSendBuffer = new byte[setting.MaxUnit];
            receiveBuffer = new byte[1 + reliableSize];
            kcpSendBuffer = new byte[1 + reliableSize];
            state = State.Disconnect;
        }

        protected void Reset(Setting config)
        {
            cookie = 0;
            pingTime = 0;
            receiveTime = 0;
            state = State.Disconnect;
            watch.Restart();

            kcp = new Kcp(0, SendReliable);
            kcp.SetMtu((uint)config.MaxUnit - Common.METADATA_SIZE);
            kcp.SetWindowSize(config.SendWindow, config.ReceiveWindow);
            kcp.SetNoDelay(config.NoDelay ? 1U : 0U, config.Interval, config.FastResend, !config.Congestion);
            kcp.dead_link = config.DeadLink;
            timeout = config.Timeout;
        }
        
        private bool TryReceive(out ReliableHeader header, out ArraySegment<byte> message)
        {
            message = default;
            header = ReliableHeader.Ping;
            var size = kcp.PeekSize();
            if (size <= 0)
            {
                return false;
            }

            if (size > receiveBuffer.Length)
            {
                Log.Error($"{GetType()}: 网络消息长度溢出 {receiveBuffer.Length} < {size}。");
                Disconnect();
                return false;
            }

            if (kcp.Receive(receiveBuffer, size) < 0)
            {
                Log.Error($"{GetType()}: 接收网络消息失败。");
                Disconnect();
                return false;
            }

            if (!Common.ParseReliable(receiveBuffer[0], out header))
            {
                Log.Error($"{GetType()}: 未知的网络消息头部 {header}");
                Disconnect();
                return false;
            }

            message = new ArraySegment<byte>(receiveBuffer, 1, size - 1);
            receiveTime = (uint)watch.ElapsedMilliseconds;
            return true;
        }

        protected void Input(int channel, ArraySegment<byte> segment)
        {
            if (channel == Channel.Reliable)
            {
                if (kcp.Input(segment.Array, segment.Offset, segment.Count) != 0)
                {
                    Log.Warn($"{GetType()}: 发送可靠消息失败。消息大小：{segment.Count - 1}");
                }
            }
            else if (channel == Channel.Unreliable)
            {
                if (segment.Count < 1) return;
                var headerByte = segment.Array[segment.Offset];
                if (!Common.ParseUnreliable(headerByte, out var header))
                {
                    Log.Error($"{GetType()}: 未知的网络消息头部 {header}");
                    Disconnect();
                    return;
                }

                if (header == UnreliableHeader.Data)
                {
                    if (state == State.Connected)
                    {
                        segment = new ArraySegment<byte>(segment.Array, segment.Offset + 1, segment.Count - 1);
                        Receive(segment, Channel.Unreliable);
                        receiveTime = (uint)watch.ElapsedMilliseconds;
                    }
                }
                else if (header == UnreliableHeader.Disconnect)
                {
                    Disconnect();
                }
            }
        }

        private void SendReliable(byte[] data, int length)
        {
            rawSendBuffer[0] = Channel.Reliable;
            Utility.Encode32U(rawSendBuffer, 1, cookie);
            Buffer.BlockCopy(data, 0, rawSendBuffer, 1 + 4, length);
            var segment = new ArraySegment<byte>(rawSendBuffer, 0, length + 1 + 4);
            Send(segment);
        }

        protected void SendReliable(ReliableHeader header, ArraySegment<byte> segment)
        {
            if (segment.Count > kcpSendBuffer.Length - 1)
            {
                Log.Error($"{GetType()}: 发送可靠消息失败。消息大小：{segment.Count}");
                return;
            }

            kcpSendBuffer[0] = (byte)header;
            if (segment.Count > 0)
            {
                Buffer.BlockCopy(segment.Array, segment.Offset, kcpSendBuffer, 1, segment.Count);
            }

            if (kcp.Send(kcpSendBuffer, 0, 1 + segment.Count) < 0)
            {
                Log.Error($"{GetType()}: 发送可靠消息失败。消息大小：{segment.Count}。");
            }
        }

        private void SendUnreliable(UnreliableHeader header, ArraySegment<byte> segment)
        {
            if (segment.Count > unreliableSize)
            {
                Log.Error($"{GetType()}: 发送不可靠消息失败。消息大小：{segment.Count}");
                return;
            }

            rawSendBuffer[0] = Channel.Unreliable;
            Utility.Encode32U(rawSendBuffer, 1, cookie);
            rawSendBuffer[5] = (byte)header;
            if (segment.Count > 0)
            {
                Buffer.BlockCopy(segment.Array, segment.Offset, rawSendBuffer, 1 + 4 + 1, segment.Count);
            }

            Send(new ArraySegment<byte>(rawSendBuffer, 0, segment.Count + 1 + 4 + 1));
        }

        public void SendData(ArraySegment<byte> data, int channel)
        {
            if (data.Count == 0)
            {
                Log.Error($"{GetType()} 尝试发送空消息。");
                Disconnect();
                return;
            }

            switch (channel)
            {
                case Channel.Reliable:
                    SendReliable(ReliableHeader.Data, data);
                    break;
                case Channel.Unreliable:
                    SendUnreliable(UnreliableHeader.Data, data);
                    break;
            }
        }

        public void Disconnect()
        {
            if (state == State.Disconnect) return;
            try
            {
                for (int i = 0; i < 5; ++i)
                {
                    SendUnreliable(UnreliableHeader.Disconnect, default);
                }
            }
            finally
            {
                state = State.Disconnect;
                Disconnected();
            }
        }

        public virtual void EarlyUpdate()
        {
            if (kcp.state == -1)
            {
                Log.Error($"{GetType()}: 网络消息被重传了 {kcp.dead_link} 次而没有得到确认！");
                Disconnect();
            }

            int total = kcp.receiveQueue.Count + kcp.sendQueue.Count + kcp.receiveBuffer.Count + kcp.sendBuffer.Count;
            if (total >= 10000)
            {
                Log.Error($"{GetType()}: 断开连接，因为它处理数据的速度不够快！");
                kcp.sendQueue.Clear();
                Disconnect();
            }

            var time = (uint)watch.ElapsedMilliseconds;
            if (time >= receiveTime + timeout)
            {
                Log.Error($"{GetType()}: 在 {timeout}ms 内没有收到任何消息后的连接超时！");
                Disconnect();
            }

            if (time >= pingTime + Common.PING_INTERVAL)
            {
                SendReliable(ReliableHeader.Ping, default);
                pingTime = time;
            }

            try
            {
                if (state == State.Connect)
                {
                    if (TryReceive(out var header, out _))
                    {
                        if (header == ReliableHeader.Connect)
                        {
                            state = State.Connected;
                            Connected();
                        }
                        else if (header == ReliableHeader.Data)
                        {
                            Log.Error($"{GetType()}: 收到未通过验证的网络消息。消息类型：{header}");
                            Disconnect();
                        }
                    }
                }
                else if (state == State.Connected)
                {
                    while (TryReceive(out var header, out var segment))
                    {
                        if (header == ReliableHeader.Connect)
                        {
                            Log.Error($"{GetType()}: 收到无效的网络消息。消息类型：{header}");
                            Disconnect();
                        }
                        else if (header == ReliableHeader.Data)
                        {
                            if (segment.Count == 0)
                            {
                                Log.Error($"{GetType()}: 收到无效的网络消息。消息类型：{header}");
                                Disconnect();
                                return;
                            }

                            Receive(segment, Channel.Reliable);
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                Log.Error($"{GetType()}: 网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
            catch (ObjectDisposedException e)
            {
                Log.Error($"{GetType()}: 网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
            catch (Exception e)
            {
                Log.Error($"{GetType()}:网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
        }

        public virtual void AfterUpdate()
        {
            try
            {
                if (state != State.Disconnect)
                {
                    kcp.Update((uint)watch.ElapsedMilliseconds);
                }
            }
            catch (SocketException e)
            {
                Log.Error($"{GetType()}: 网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
            catch (ObjectDisposedException e)
            {
                Log.Error($"{GetType()}: 网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
            catch (Exception e)
            {
                Log.Error($"{GetType()}: 网络发生异常，断开连接。\n{e}");
                Disconnect();
            }
        }

        protected abstract void Connected();
        protected abstract void Send(ArraySegment<byte> segment);
        protected abstract void Receive(ArraySegment<byte> message, int channel);
        protected abstract void Disconnected();
    }
}