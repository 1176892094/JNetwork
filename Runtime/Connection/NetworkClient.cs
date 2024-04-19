using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 仅在服务器上被创建
    /// </summary>
    public sealed class NetworkClient : NetworkProxy
    {
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
        public readonly int clientId;

        /// <summary>
        /// 是否生成 PlayerPrefab
        /// </summary>
        internal bool isSpawn;

        /// <summary>
        /// 远端时间线
        /// </summary>
        [ShowInInspector] internal double remoteTimeline;

        /// <summary>
        /// 远端时间量程
        /// </summary>
        [ShowInInspector] private double remoteTimescale;

        /// <summary>
        /// 缓存时间的倍率
        /// </summary>
        private double bufferTimeMultiplier = 2;

        /// <summary>
        /// 快照缓存极限大小
        /// </summary>
        internal readonly int snapshotBufferSizeLimit = 64;

        /// <summary>
        /// 缓存时间
        /// </summary>
        private double bufferTime => NetworkManager.Instance.sendRate * bufferTimeMultiplier;

        /// <summary>
        /// 初始化设置客户端Id
        /// </summary>
        /// <param name="clientId">传入客户端的Id</param>
        public NetworkClient(int clientId)
        {
            this.clientId = clientId;
            driftEma = new NetworkAverage(NetworkManager.Instance.tickRate * NetworkManager.Setting.driftEmaDuration);
            deliveryTimeEma = new NetworkAverage(NetworkManager.Instance.tickRate * NetworkManager.Setting.deliveryTimeEmaDuration);
            snapshotBufferSizeLimit = Mathf.Max((int)NetworkManager.Setting.bufferTimeMultiplier, snapshotBufferSizeLimit);
        }

        /// <summary>
        /// 接收到快照消息
        /// </summary>
        /// <param name="snapshot"></param>
        internal void OnSnapshotMessage(SnapshotTime snapshot)
        {
            if (snapshots.Count >= snapshotBufferSizeLimit) return;
            if (NetworkManager.Setting.dynamicAdjustment)
            {
                bufferTimeMultiplier = SnapshotUtils.DynamicAdjust(NetworkManager.Instance.sendRate, deliveryTimeEma.deviation,
                    NetworkManager.Setting.dynamicAdjustmentTolerance);
            }

            SnapshotUtils.InsertAndAdjust(snapshots, snapshot, ref remoteTimeline, ref remoteTimescale, NetworkManager.Instance.sendRate,
                bufferTime, ref driftEma, ref deliveryTimeEma);
        }

        /// <summary>
        /// 差值更新
        /// </summary>
        public void UpdateInterpolation()
        {
            if (snapshots.Count > 0)
            {
                SnapshotUtils.StepTime(Time.unscaledDeltaTime, ref remoteTimeline, remoteTimescale);
                SnapshotUtils.StepInterpolation(snapshots, remoteTimeline);
            }
        }

        /// <summary>
        /// 服务器更新
        /// </summary>
        internal override void OnUpdate()
        {
            SendRpc(reliableRpc, Channel.Reliable);
            SendRpc(unreliableRpc, Channel.Unreliable);
            base.OnUpdate();
        }

        /// <summary>
        /// 处理Rpc事件
        /// </summary>
        /// <param name="writer">Rpc信息</param>
        /// <param name="channel">传输通道</param>
        private void SendRpc(NetworkWriter writer, Channel channel)
        {
            if (writer.position <= 0) return;
            Send(new InvokeRpcMessage(writer), channel);
            writer.position = 0;
        }

        /// <summary>
        /// 对Rpc的缓存
        /// </summary>
        /// <param name="message"></param>
        /// <param name="buffer"></param>
        /// <param name="channel"></param>
        /// <param name="maxMessageSize"></param>
        private void SendRpc(ClientRpcMessage message, NetworkWriter buffer, Channel channel, int maxMessageSize)
        {
            int bufferLimit = maxMessageSize - NetworkConst.MessageSize - sizeof(int) - NetworkConst.HeaderSize;
            int before = buffer.position;
            buffer.Write(message);
            int messageSize = buffer.position - before;
            if (messageSize > bufferLimit)
            {
                Debug.LogWarning($"远程调用 {message.objectId} 消息大小不能超过 {bufferLimit}。消息大小：{messageSize}");
                return;
            }

            if (buffer.position > bufferLimit)
            {
                buffer.position = before;
                SendRpc(buffer, channel);
                buffer.Write(message);
            }
        }

        /// <summary>
        /// 由NetworkBehaviour调用
        /// </summary>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        internal void InvokeRpc(ClientRpcMessage message, Channel channel)
        {
            int maxSize = NetworkManager.Transport.GetMaxPacketSize(channel);
            switch (channel)
            {
                case Channel.Reliable:
                    SendRpc(message, reliableRpc, Channel.Reliable, maxSize);
                    break;
                case Channel.Unreliable:
                    SendRpc(message, unreliableRpc, Channel.Unreliable, maxSize);
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
            if (clientId == NetworkConst.HostId)
            {
                var writer = NetworkWriter.Pop();
                writer.WriteBytesInternal(segment.Array, segment.Offset, segment.Count);
                NetworkManager.Client.connection.writerQueue.Enqueue(writer);
                return;
            }

            GetWriterPack(channel).WriteEnqueue(segment, NetworkManager.Time.localTime);
        }

        /// <summary>
        /// 服务器发送到传输
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="channel">传输通道</param>
        protected override void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            NetworkManager.Transport.ServerSend(clientId, segment, channel);
        }

        /// <summary>
        /// 客户端断开连接
        /// </summary>
        public override void Disconnect()
        {
            isReady = false;
            NetworkManager.Transport.ServerDisconnect(clientId);
        }
    }
}