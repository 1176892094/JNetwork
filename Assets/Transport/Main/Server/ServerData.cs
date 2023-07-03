using System;

namespace Transport
{
    public readonly struct ServerData
    {
        public readonly Action<int> onConnected;
        public readonly Action<int> onDisconnected;
        public readonly Action<int, ArraySegment<byte>, Channel> onReceive;

        public ServerData(Action<int> onConnected, Action<int> onDisconnected, Action<int, ArraySegment<byte>, Channel> onReceive)
        {
            this.onConnected = onConnected;
            this.onDisconnected = onDisconnected;
            this.onReceive = onReceive;
        }
    }
}