using System;

namespace Transport
{
    public readonly struct ClientData
    {
        public readonly Action onConnected;
        public readonly Action onDisconnected;
        public readonly Action<ArraySegment<byte>> onReceive;

        public ClientData(Action onConnected, Action onDisconnected, Action<ArraySegment<byte>> onReceive)
        {
            this.onConnected = onConnected;
            this.onDisconnected = onDisconnected;
            this.onReceive = onReceive;
        }
    }
}