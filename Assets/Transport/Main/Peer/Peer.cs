using System;
using System.Diagnostics;

namespace Transport
{
    internal class Peer
    {
        private uint lastReceiveTime;
        private State state;
        private readonly Jdp jdp;
        private readonly uint cookie;
        private readonly int timeout;
        private readonly int reliableSize;
        private readonly int unreliableSize;
        private readonly byte[] receiveCookie;
        private readonly byte[] messageBuffer;
        private readonly byte[] kcpSendBuffer;
        private readonly byte[] rawSendBuffer;
        private readonly Stopwatch watch = new Stopwatch();
        private readonly PeerData peerData;

        public Peer(PeerData peerData, Setting setting, uint cookie)
        {
            this.cookie = cookie;
            this.peerData = peerData;
            timeout = setting.timeout;
            jdp = new Jdp(0, SendReliable);
            reliableSize = Utils.ReliableSize(setting.maxTransferUnit, setting.receivePacketSize);
            unreliableSize = Utils.UnreliableSize(setting.maxTransferUnit);
            receiveCookie = new byte[4];
            messageBuffer = new byte[reliableSize + 1];
            kcpSendBuffer = new byte[reliableSize + 1];
            rawSendBuffer = new byte[setting.maxTransferUnit];
            watch.Start();
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
            }
            catch
            {
                // ignored
            }

            state = State.Disconnected;
            peerData.onDisconnected?.Invoke();
        }

        /// <summary>
        /// 发送传输信息
        /// </summary>
        public void Send(ArraySegment<byte> segment, Channel channel)
        {
            if (segment.Count == 0)
            {
                Log.Error("The peer tried sending empty message.");
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
            if (segment.Count > kcpSendBuffer.Length - 1)
            {
                Log.Error($"Failed to send reliable message of size {segment.Count}");
                return;
            }

            kcpSendBuffer[0] = (byte)header; //设置传输的头部
            if (segment.Count > 0)
            {
                Buffer.BlockCopy(segment.Array, segment.Offset, kcpSendBuffer, 1, segment.Count);
            }

            int sent = jdp.Send(kcpSendBuffer, 0, segment.Count + 1);
            if (sent < 0)
            {
                Log.Error($"Send failed with error = {sent} for content with length = {segment.Count}");
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
            peerData.onSend(segment);
        }

        /// <summary>
        /// 不可靠传输
        /// </summary>
        private void SendUnreliable(ArraySegment<byte> segment)
        {
            if (segment.Count > unreliableSize)
            {
                Log.Error($"Failed to send unreliable message of size {segment.Count}");
                return;
            }

            rawSendBuffer[0] = (byte)Channel.Unreliable;
            Buffer.BlockCopy(receiveCookie, 0, rawSendBuffer, 1, 4);
            Buffer.BlockCopy(segment.Array, segment.Offset, rawSendBuffer, 1 + 4, segment.Count);
            var message = new ArraySegment<byte>(rawSendBuffer, 0, segment.Count + 1 + 4);
            peerData.onSend(message);
        }

        /// <summary>
        /// 握手请求
        /// </summary>
        public void SendHandshake()
        {
            var cookieBytes = BitConverter.GetBytes(cookie);
            Log.Info($"Sending handshake to other end with cookie = {cookie}!");
            var segment = new ArraySegment<byte>(cookieBytes);
            SendReliable(Header.Handshake, segment);
        }

        public void BeforeUpdate()
        {
            
        }

        public void AfterUpdate()
        {
            
        }
    }
}