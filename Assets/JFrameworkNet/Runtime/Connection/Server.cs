using System;
using System.Collections.Generic;
using JFramework.Udp;

namespace JFramework.Net
{
    public class Server : Connection
    {
        internal readonly Queue<NetworkWriterObject> writeQueue = new Queue<NetworkWriterObject>();
        internal bool connecting;
        internal bool disconnecting;
        
        protected override void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            Transport.Instance.ClientSend(segment, channel);
        }
        
        internal override void Update()
        {
            base.Update();
            if (connecting)
            {
                connecting = false;
                NetworkClient.OnConnected?.Invoke();
            }
            
            while (writeQueue.Count > 0)
            {
                var writer = writeQueue.Dequeue();
                var segment = writer.ToArraySegment();
                var batch = GetBatch(Channel.Reliable);
                batch.WriteEnqueue(segment, NetworkTime.localTime);
                using (var batchWriter = NetworkWriterPool.Pop())
                {
                    if (batch.WriteDequeue(batchWriter))
                    {
                        NetworkClient.OnClientReceive(batchWriter.ToArraySegment(), Channel.Reliable);
                    }
                }

                NetworkWriterPool.Push(writer);
            }

            if (!disconnecting) return;
            disconnecting = false;
            NetworkClient.OnDisconnected?.Invoke();
        }
        
        public override void Disconnect()
        {
            isReady = false;
            NetworkClient.isReady = false;
        }
    }
}