using System;
using JFramework.Udp;

namespace JFramework.Net
{
    public sealed class Client : Connection
    {
        public bool isLocal;

        public Client(int clientId) : base(clientId)
        {
        }

        protected override void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            Transport.Instance.ServerSend(clientId, segment, channel);
        }

        public override void Disconnect()
        {
            
        }
    }
}