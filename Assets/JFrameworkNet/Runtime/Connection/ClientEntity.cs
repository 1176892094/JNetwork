using System;
using System.Collections.Generic;
using JFramework.Udp;

namespace JFramework.Net
{
    public sealed class ClientEntity : Connection
    {
        internal ServerEntity connection;
        internal readonly NetworkReceive receive = new NetworkReceive();
        internal readonly HashSet<NetworkObject> observing = new HashSet<NetworkObject>();
        public readonly int clientId;

        public ClientEntity(int clientId) => this.clientId = clientId;

        protected override void AddToQueue(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            if (isLocal)
            {
                NetworkWriter writer = NetworkWriterPool.Pop();
                writer.WriteBytesInternal(segment.Array, segment.Offset, segment.Count);
                connection.writeQueue.Enqueue(writer);
            }
            else
            {
                base.AddToQueue(segment, channel);
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