using System;
using Sirenix.OdinInspector;

namespace JFramework.Net
{
    /// <summary>
    /// 仅在服务器上被创建
    /// </summary>
    public sealed class ClientEntity : Connection
    {
        /// <summary>
        /// 客户端的Id
        /// </summary>
        [ShowInInspector] public readonly int clientId;

        /// <summary>
        /// 是主机客户端
        /// </summary>
        [ShowInInspector] private readonly bool isHost;

        /// <summary>
        /// 网络消息读取
        /// </summary>
        [ShowInInspector] internal readonly NetworkReaders readers = new NetworkReaders();


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