using System;
using System.Net.Sockets;
// ReSharper disable All

namespace JFramework.Udp
{
    internal sealed partial class Peer
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
                Log.Error($"P2P发生异常，断开连接。\n{e}.");
                Disconnect();
            }
            catch (ObjectDisposedException e)
            {
                Log.Error($"P2P发生异常，断开连接。\n{e}.");
                Disconnect();
            }
            catch (Exception e)
            {
                Log.Error($"P2P发生异常，断开连接。\n{e}.");
                Disconnect();
            }
        }
        
        public void AfterUpdate()
        {
            try
            {
                if (state == State.Connected || state == State.Authority)
                {
                    jdp.Update(watch.ElapsedMilliseconds);
                }
            }
            catch (SocketException e)
            {
                Log.Error($"P2P发生异常，断开连接。\n{e}.");
                Disconnect();
            }
            catch (ObjectDisposedException e)
            {
                Log.Error($"P2P发生异常，断开连接。\n{e}.");
                Disconnect();
            }
            catch (Exception e)
            {
                Log.Error($"P2P发生异常，断开连接。\n{e}.");
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

                        Buffer.BlockCopy(segment.Array, segment.Offset, receiveCookie, 0, 4);
                        var prettyCookie = BitConverter.ToUInt32(segment.Array, segment.Offset);
                        Log.Info($"接收到握手消息。签名缓存：{prettyCookie}");
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
                        Log.Warn($"身份验证时收到无效的消息。消息类型：{header}");
                        Disconnect();
                        break;
                    case Header.Message:
                        if (segment.Count > 0)
                        {
                            OnReceive?.Invoke(segment, Channel.Reliable);
                        }
                        else
                        {
                            Log.Error("通过身份验证时收到空数据消息。");
                            Disconnect();
                        }

                        break;
                    case Header.Disconnect:
                        Log.Info($"接收到断开连接的消息。");
                        Disconnect();
                        break;
                }
            }
        }
        
        private void OnEarlyUpdate(uint time)
        {
            if (time >= lastReceiveTime + timeout)
            {
                Log.Error($"在 {timeout}ms 内没有收到任何消息后的连接超时！");
                Disconnect();
            }

            if (jdp.state == -1)
            {
                Log.Error($"消息被重传了 {jdp.deadLink} 次而没有得到确认！");
                Disconnect();
            }

            if (time >= lastPingTime + Helper.PING_INTERVAL)
            {
                SendReliable(Header.Ping, default);
                lastPingTime = time;
            }
            
            if (jdp.GetBufferQueueCount() >= Helper.QUEUE_DISCONNECTED_THRESHOLD)
            {
                Log.Error($"断开连接，因为它处理数据的速度不够快！");
                jdp.sendQueue.Clear();
                Disconnect();
            }
        }
    }
}