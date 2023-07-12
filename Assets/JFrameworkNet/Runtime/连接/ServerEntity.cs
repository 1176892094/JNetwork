using System;
using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    public class ServerEntity : Connection
    {
        /// <summary>
        /// 存储写入队列的字典
        /// </summary>
        internal readonly Queue<NetworkWriter> writeQueue = new Queue<NetworkWriter>();
        
        /// <summary>
        /// 客户端发送到传输
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="channel">传输通道</param>
        protected override void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            Transport.current.ClientSend(segment, channel);
        }

        /// <summary>
        /// 重写Update方法
        /// </summary>
        internal override void Update()
        {
            base.Update();
            LocalUpdate();
        }

        /// <summary>
        /// 本地更新
        /// </summary>
        private void LocalUpdate()
        {
            if (!ServerManager.isHost) return;
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
                        ClientManager.OnClientReceive(sendWriter.ToArraySegment(), Channel.Reliable);
                    }
                }

                NetworkWriter.Push(writer);
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="channel">传输通道</param>
        internal override void Send(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            if (ServerManager.isHost)
            {
                if (segment.Count == 0)
                {
                    Debug.LogError("发送消息大小不能为零！");
                    return;
                }

                var send = GetWriters(channel);
                send.WriteEnqueue(segment, NetworkTime.localTime); // 添加到队列末尾并写入数据

                using var writer = NetworkWriter.Pop();
                if (send.WriteDequeue(writer)) // 尝试从队列中取出元素并写入到目标
                {
                    ServerManager.OnServerReceive(NetworkConst.HostId, writer.ToArraySegment(), channel);
                }
                else
                {
                    Debug.LogError("客户端不能获取写入器。");
                }
            }
            else
            {
                base.Send(segment, channel);
            }
        }

        /// <summary>
        /// 服务器断开连接
        /// </summary>
        public override void Disconnect()
        {
            isReady = false;
            ClientManager.isReady = false;
            Transport.current.ClientDisconnect();
        }
    }
}