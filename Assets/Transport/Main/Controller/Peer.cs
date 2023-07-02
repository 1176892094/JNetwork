using System;

namespace Transport
{
    public class Peer
    {
        private State state;
        private readonly uint cookie;
        private readonly int timeout;
        private readonly int reliableSize;
        private readonly int unreliableSize;
        private readonly byte[] receiveCookie;
        private readonly byte[] messageBuffer;
        private readonly byte[] kcpSendBuffer;
        private readonly byte[] rawSendBuffer;
        private readonly PeerData peerData;

        public Peer(PeerData peerData, Setting setting, uint cookie)
        {
            this.cookie = cookie;
            this.peerData = peerData;
            timeout = setting.timeout;

            reliableSize = Utils.ReliableSize(setting.maxTransferUnit, setting.packageReceive);
            unreliableSize = Utils.UnreliableSize(setting.maxTransferUnit);
            receiveCookie = new byte[4];
            messageBuffer = new byte[reliableSize + 1];
            kcpSendBuffer = new byte[reliableSize + 1];
            rawSendBuffer = new byte[setting.maxTransferUnit];
        }


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

        public void Send(ArraySegment<byte> segment)
        {
        }

        private void SendReliable(Header header, ArraySegment<byte> segment)
        {
            if (kcpSendBuffer.Length - 1 < segment.Count)
            {
                Log.Info($"Failed to send reliable message of size {segment.Count}");
                return;
            }

            kcpSendBuffer[0] = (byte)header;
            if (segment.Count > 0 && segment.Array != null)
            {
                Buffer.BlockCopy(segment.Array, segment.Offset, kcpSendBuffer, 1, segment.Count);
            }

            // int sent = jdp.Send(kcpSendBuffer, 0, 1 + segment.Count);
            // if (sent < 0)
            // {
            //     Log.Info($"Send failed with error = {sent} for content with length = {segment.Count}");
            // }
        }

        public void SendHandshake()
        {
            var cookieBytes = BitConverter.GetBytes(cookie);
            Log.Info($"Sending handshake to other end with cookie = {cookie}!");
            SendReliable(Header.Handshake, new ArraySegment<byte>(cookieBytes));
        }
    }
}