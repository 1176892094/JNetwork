using System;

namespace JFNet.JDP
{
    public readonly struct ClientData
    {
        public readonly Action onConnected;
        public readonly Action onDisconnected;
        public readonly Action<ArraySegment<byte>, Channel> onReceive;

        public ClientData(Action onConnected, Action onDisconnected, Action<ArraySegment<byte>, Channel> onReceive)
        {
            this.onConnected = onConnected;
            this.onDisconnected = onDisconnected;
            this.onReceive = onReceive;
        }
    }
}