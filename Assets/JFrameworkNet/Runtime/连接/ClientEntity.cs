using System;
using System.Collections.Generic;
using JFramework.Udp;

namespace JFramework.Net
{
    public sealed class ClientEntity : Connection
    {
        internal ServerEntity connection;
        internal readonly NetworkReaders readers = new NetworkReaders();
        internal readonly HashSet<NetworkObject> observing = new HashSet<NetworkObject>();
        public readonly int clientId;

        public ClientEntity(int clientId) => this.clientId = clientId;

        internal override void Send(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            if (isLocal)
            {
                NetworkWriter writer = NetworkWriter.Pop();
                writer.WriteBytesInternal(segment.Array, segment.Offset, segment.Count);
                connection.writeQueue.Enqueue(writer);
            }
            else
            {
                base.Send(segment, channel);
            }
        }

        protected override void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            Transport.current.ServerSend(clientId, segment, channel);
        }

        public override void Disconnect()
        {
            isReady = false;
            Transport.current.ServerDisconnect(clientId);
        }
    }
}