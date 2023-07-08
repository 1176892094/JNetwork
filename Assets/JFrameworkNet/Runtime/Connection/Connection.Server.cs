using System;
using System.Collections.Generic;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public class ServerConnection : Connection
    {
        internal readonly Queue<NetworkWriter> writeQueue = new Queue<NetworkWriter>();
        
        internal bool connecting;
        internal bool disconnecting;
        
        protected override void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            Transport.current.ClientSend(segment, channel);
        }
        
        internal override void Update()
        {
            base.Update();
            if (!isLocal) return;
            if (connecting)
            {
                connecting = false;
                NetworkClient.OnConnected?.Invoke();
            }
            
            while (writeQueue.Count > 0)
            {
                var writer = writeQueue.Dequeue();
                var segment = writer.ToArraySegment();
                var batch = GetNetworkSend(Channel.Reliable);
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

        internal override void Send(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            if (isLocal)
            {
                if (segment.Count == 0)
                {
                    Debug.LogError("Segment cannot send 0 bytes");
                    return;
                }
                
                var send = GetNetworkSend(channel);
                send.WriteEnqueue(segment, NetworkTime.localTime); // 添加到队列末尾并写入数据

                using var writer = NetworkWriterPool.Pop();
                if (send.WriteDequeue(writer)) // 尝试从队列中取出元素并写入到目标
                {
                    NetworkServer.OnServerReceive(clientId, writer.ToArraySegment(), channel);
                }
                else
                {
                    Debug.LogError("Connection failed to make writer.");
                }
            }
            else
            {
                base.Send(segment,channel);
            }
        }

        public override void Disconnect()
        {
            isReady = false;
            NetworkClient.isReady = false;
            Transport.current.ClientDisconnect();
        }
    }
}