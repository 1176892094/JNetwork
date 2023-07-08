using System;
using System.Collections.Generic;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public sealed class ClientObject : Connection
    {
        internal ServerObject connection;
        public readonly NetworkReceive receive = new NetworkReceive();
        public readonly HashSet<NetworkObject> observing = new HashSet<NetworkObject>();
        public int clientId;

        public ClientObject(int clientId) => this.clientId = clientId;


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