using System;
using System.Collections.Generic;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public class ServerEntity : Connection
    {
        internal readonly Queue<NetworkWriter> writeQueue = new Queue<NetworkWriter>();

        protected override void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            Transport.current.ClientSend(segment, channel);
        }

        internal override void Update()
        {
            base.Update();
            LocalUpdate();
        }

        private void LocalUpdate()
        {
            if (!isLocal) return;
            while (writeQueue.Count > 0)
            {
                var writer = writeQueue.Dequeue();
                var segment = writer.ToArraySegment();
                var send = GetWriters(Channel.Reliable);
                send.WriteEnqueue(segment, NetworkTime.localTime);
                using (var sendWriter = NetworkWriter.Pop())
                {
                    if (send.WriteDequeue(sendWriter))
                    {
                        NetworkClient.OnClientReceive(sendWriter.ToArraySegment(), Channel.Reliable);
                    }
                }

                NetworkWriter.Push(writer);
            }
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

                var send = GetWriters(channel);
                send.WriteEnqueue(segment, NetworkTime.localTime); // 添加到队列末尾并写入数据

                using var writer = NetworkWriter.Pop();
                if (send.WriteDequeue(writer)) // 尝试从队列中取出元素并写入到目标
                {
                    NetworkServer.OnServerReceive(NetworkConst.HostId, writer.ToArraySegment(), channel);
                }
                else
                {
                    Debug.LogError("Connection failed to make writer.");
                }
            }
            else
            {
                base.Send(segment, channel);
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