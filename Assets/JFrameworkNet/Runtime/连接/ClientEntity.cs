using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 仅在服务器上被创建
    /// </summary>
    public sealed class ClientEntity : Connection
    {
        /// <summary>
        /// 网络消息读取
        /// </summary>
        [ShowInInspector] internal readonly NetworkReaders readers = new NetworkReaders();
        
        /// <summary>
        /// 可靠Rpc列表
        /// </summary>
        private readonly NetworkWriter reliableRpc = new NetworkWriter();
        
        /// <summary>
        /// 不可靠Rpc列表
        /// </summary>
        private readonly NetworkWriter unreliableRpc = new NetworkWriter();
        
        /// <summary>
        /// 客户端的Id
        /// </summary>
        [ShowInInspector] public readonly int clientId;

        /// <summary>
        /// 是主机客户端
        /// </summary>
        [ShowInInspector] private readonly bool isHost;

        /// <summary>
        /// 初始化设置客户端Id
        /// </summary>
        /// <param name="clientId">传入客户端的Id</param>
        public ClientEntity(int clientId)
        {
            this.clientId = clientId;
            isHost = clientId == NetworkConst.HostId;
        }

        /// <summary>
        /// 服务器更新
        /// </summary>
        internal override void Update()
        {
            FlushRpc(reliableRpc, Channel.Reliable);
            FlushRpc(unreliableRpc, Channel.Unreliable);
            base.Update();
        }

        /// <summary>
        /// 处理Rpc事件
        /// </summary>
        /// <param name="writer">Rpc信息</param>
        /// <param name="channel">传输通道</param>
        private void FlushRpc(NetworkWriter writer, Channel channel)
        {
            if (writer.position <= 0) return;
            Send(new RpcBufferEvent((ArraySegment<byte>)writer), channel);
            writer.position = 0;
        }
        
        /// <summary>
        /// 对Rpc的缓存
        /// </summary>
        /// <param name="event"></param>
        /// <param name="buffer"></param>
        /// <param name="channel"></param>
        /// <param name="maxMessageSize"></param>
        private void BufferRpc(ClientRpcEvent @event, NetworkWriter buffer, Channel channel, int maxMessageSize)
        {
            int bufferLimit = maxMessageSize - NetworkConst.EventSize - sizeof(int) - NetworkConst.HeaderSize;
            int before = buffer.position;
            buffer.Write(@event);
            int messageSize = buffer.position - before;
            if (messageSize > bufferLimit)
            {
                Debug.LogWarning($"远程调用 {@event.netId} 消息大小不能超过 {bufferLimit}。消息大小：{messageSize}");
                return;
            }
            
            if (buffer.position > bufferLimit)
            {
                buffer.position = before;
                FlushRpc(buffer, channel);
                buffer.Write(@event);
            }
        }
        
        /// <summary>
        /// TODO:有NetworkEntity调用
        /// </summary>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        internal void BufferRpc(ClientRpcEvent message, Channel channel)
        {
            int maxSize = Transport.current.GetMaxPacketSize(channel);
            switch (channel)
            {
                case Channel.Reliable:
                    BufferRpc(message, reliableRpc, Channel.Reliable, maxSize);
                    break;
                case Channel.Unreliable:
                    BufferRpc(message, unreliableRpc, Channel.Unreliable, maxSize);
                    break;
            }
        }
        
        /// <summary>
        /// 客户端向服务器发送消息
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="channel">传输通道</param>
        internal override void Send(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            if (isHost)
            {
                NetworkWriter writer = NetworkWriter.Pop();
                writer.WriteBytesInternal(segment.Array, segment.Offset, segment.Count);
                ClientManager.connection.writeQueue.Enqueue(writer);
                return;
            }

            base.Send(segment, channel);
        }

        /// <summary>
        /// 服务器发送到传输
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="channel">传输通道</param>
        protected override void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            Transport.current.ServerSend(clientId, segment, channel);
        }

        /// <summary>
        /// 客户端断开连接
        /// </summary>
        public override void Disconnect()
        {
            isReady = false;
            Transport.current.ServerDisconnect(clientId);
        }
    }
}