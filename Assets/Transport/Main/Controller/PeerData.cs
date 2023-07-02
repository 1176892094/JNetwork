using System;

namespace Transport
{
    public readonly struct PeerData
    {
        public readonly Action onAuthority;
        public readonly Action onDisconnected;
        public readonly Action<ArraySegment<byte>> onSend;
        public readonly Action<ArraySegment<byte>> onReceive;

        public PeerData(Action onAuthority, Action onDisconnected, Action<ArraySegment<byte>> onSend, Action<ArraySegment<byte>> onReceive)
        {
            this.onAuthority = onAuthority;
            this.onDisconnected = onDisconnected;
            this.onSend = onSend;
            this.onReceive = onReceive;
        }
    }
}