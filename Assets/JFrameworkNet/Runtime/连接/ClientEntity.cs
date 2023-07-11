using System;
using System.Collections.Generic;
using JFramework.Udp;

namespace JFramework.Net
{
    public sealed class ClientEntity : Connection
    {
        /// <summary>
        /// 连接到的Server
        /// </summary>
        internal ServerEntity connection;

        /// <summary>
        /// 网络消息读取
        /// </summary>
        internal readonly NetworkReaders readers = new NetworkReaders();

        /// <summary>
        /// 客户端所观察的网络游戏对象
        /// </summary>
        internal readonly HashSet<NetworkObject> observing = new HashSet<NetworkObject>();

        /// <summary>
        /// 客户端的Id
        /// </summary>
        public readonly int clientId;

        /// <summary>
        /// 初始化设置客户端Id
        /// </summary>
        /// <param name="clientId">传入客户端的Id</param>
        public ClientEntity(int clientId) => this.clientId = clientId;

        /// <summary>
        /// 客户端向服务器发送消息
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="channel">传输通道</param>
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

        internal void AddToObserver(NetworkObject @object)
        {
            observing.Add(@object);
            NetworkServer.SendSpawnMessage(this, @object);
        }
    }
}