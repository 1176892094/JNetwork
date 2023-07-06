using System;
using JFramework.Udp;

namespace JFramework.Net
{
    public sealed class ClientConnection : Connection
    {
        public bool isLocal;
        public NetworkReceive receive = new NetworkReceive();

        public ClientConnection(int clientId) : base(clientId)
        {
        }

        protected override void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            Transport.current.ServerSend(clientId, segment, channel);
        }

        public override void Disconnect()
        {
            
        }
    }
}