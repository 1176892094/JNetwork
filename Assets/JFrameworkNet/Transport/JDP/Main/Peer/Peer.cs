using System;
using System.Diagnostics;
// ReSharper disable All

namespace JFramework.Udp
{
    internal sealed partial class Peer
    {
        private State state;
        private uint lastPingTime;
        private uint lastReceiveTime;
        private readonly Jdp jdp;
        private readonly uint cookie;
        private readonly int timeout;
        private readonly int reliableSize;
        private readonly int unreliableSize;
        private readonly byte[] messageBuffer;
        private readonly byte[] jdpSendBuffer;
        private readonly byte[] rawSendBuffer;
        private readonly byte[] receiveCookie = new byte[4];
        private readonly Stopwatch watch = new Stopwatch();
        private readonly PeerData peerData;

        public Peer(PeerData peerData, Setting setting, uint cookie)
        {
            this.cookie = cookie;
            this.peerData = peerData;
            timeout = setting.timeout;
            jdp = new Jdp(0, SendReliable);
            jdp.SetNoDelay(setting.noDelay ? 1U : 0U, setting.interval, setting.resend, setting.congestion);
            jdp.SetWindowSize(setting.sendPacketSize, setting.receivePacketSize);
            jdp.SetTransferUnit((uint)setting.maxTransferUnit - Utils.METADATA_SIZE);
            reliableSize = Utils.ReliableSize(setting.maxTransferUnit, setting.receivePacketSize);
            unreliableSize = Utils.UnreliableSize(setting.maxTransferUnit);
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
            if (segment.Count > jdpSendBuffer.Length - 1)
            {
                Log.Error($"Failed to send reliable message of size {segment.Count}");
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
                Log.Error($"The length of {messageSize} is not allowed to be greater than {messageBuffer.Length}.");
                Disconnect();
                return false;
            }

            int received = jdp.Receive(messageBuffer, messageSize);
            if (received < 0)
            {
                Log.Error($"Receive failed with error = {received}");
                Disconnect();
                return false;
            }

            header = (Header)messageBuffer[0];
            segment = new ArraySegment<byte>(messageBuffer, 1, messageSize - 1);
            lastReceiveTime = (uint)watch.ElapsedMilliseconds;
            return true;
        }

        public void Input(ArraySegment<byte> segment)
        {
            if (segment.Count <= 5) return;
            var channel = segment.Array[segment.Offset];
            var newCookie = BitConverter.ToUInt32(segment.Array, segment.Offset + 1);

            if (state == State.Authority && newCookie != cookie)
            {
                Log.Warn($"The peer dropped message with invalid cookie: {newCookie} - {cookie}.");
                return;
            }

            var message = new ArraySegment<byte>(segment.Array, segment.Offset + 1 + 4, segment.Count - 1 - 4);

            switch (channel)
            {
                case (byte)Channel.Reliable:
                    OnInputReliable(message);
                    break;
                case (byte)Channel.Unreliable:
                    OnInputUnreliable(message);
                    break;
                default:
                    Log.Warn($"The peer invalid channel header: {channel}");
                    break;
            }
        }

        private void OnInputReliable(ArraySegment<byte> message)
        {
            int input = jdp.Input(message.Array, message.Offset, message.Count);
            if (input != 0)
            {
                Log.Warn($"Input failed with error = {input} for buffer with length = {message.Count - 1}");
            }
        }

        private void OnInputUnreliable(ArraySegment<byte> message)
        {
            if (state == State.Authority)
            {
                peerData.onReceive?.Invoke(message, Channel.Unreliable);
                lastReceiveTime = (uint)watch.ElapsedMilliseconds;
            }
            else
            {
                Log.Warn($"The kcp peer received unreliable message while not authenticated.");
            }
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
    }
}