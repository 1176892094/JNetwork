using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 仅在主机或者客户端模式下创建
    /// </summary>
    public class NetworkServer : NetworkProxy
    {
        /// <summary>
        /// 存储写入队列的字典
        /// </summary>
        internal readonly Queue<NetworkWriter> writeQueue = new Queue<NetworkWriter>();

        /// <summary>
        /// 网络设置
        /// </summary>
        private readonly SettingManager setting;

        /// <summary>
        /// 当前时间线
        /// </summary>
        [ShowInInspector] internal double localTimeline;

        /// <summary>
        /// 当前时间量程
        /// </summary>
        private double localTimescale = 1;

        /// <summary>
        /// 缓存时间
        /// </summary>
        private double bufferTime => NetworkManager.Instance.sendRate * setting.bufferTimeMultiplier;

        /// <summary>
        /// 构造函数初始化
        /// </summary>
        public NetworkServer()
        {
            setting = NetworkManager.Setting;
            driftEma = new NetworkAverage(NetworkManager.Instance.tickRate * setting.driftEmaDuration);
            deliveryTimeEma = new NetworkAverage(NetworkManager.Instance.tickRate * setting.deliveryTimeEmaDuration);
        }

        /// <summary>
        /// 快照处理
        /// </summary>
        /// <param name="snapshot">新的快照</param>
        internal void OnSnapshotMessage(SnapshotTime snapshot)
        {
            if (setting.dynamicAdjustment)
            {
                setting.bufferTimeMultiplier = SnapshotUtils.DynamicAdjust(NetworkManager.Instance.sendRate, deliveryTimeEma.deviation,
                    setting.dynamicAdjustmentTolerance);
            }

            SnapshotUtils.InsertAndAdjust(snapshots, snapshot, ref localTimeline, ref localTimescale, NetworkManager.Instance.sendRate,
                bufferTime, ref driftEma, ref deliveryTimeEma);
        }

        /// <summary>
        /// 快照更新
        /// </summary>
        public void UpdateInterpolation()
        {
            if (snapshots.Count > 0)
            {
                SnapshotUtils.StepTime(Time.unscaledDeltaTime, ref localTimeline, localTimescale);
                SnapshotUtils.StepInterpolation(snapshots, localTimeline);
            }
        }

        /// <summary>
        /// 客户端发送到传输
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="channel">传输通道</param>
        protected override void SendToTransport(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            NetworkManager.Transport.ClientSend(segment, channel);
        }

        /// <summary>
        /// 重写Update方法
        /// </summary>
        internal override void OnUpdate()
        {
            base.OnUpdate();
            LocalUpdate();
        }

        /// <summary>
        /// 本地更新
        /// </summary>
        private void LocalUpdate()
        {
            if (NetworkManager.Instance.mode != NetworkMode.Host) return;
            while (writeQueue.Count > 0)
            {
                using var writer = writeQueue.Dequeue(); // 从队列中取出
                var writers = GetWriterPack(Channel.Reliable); // 获取可靠传输
                writers.WriteEnqueue(writer, NetworkManager.Time.localTime); // 将数据写入到队列
                using var data = NetworkWriter.Pop(); // 取出新的 writer
                if (writers.WriteDequeue(data)) // 将 writer 拷贝到 data
                {
                    NetworkManager.Client.OnClientReceive(data, Channel.Reliable);
                }
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="channel">传输通道</param>
        internal override void Send(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            if (NetworkManager.Instance.mode is NetworkMode.Host)
            {
                if (segment.Count == 0)
                {
                    Debug.LogError("发送消息大小不能为零！");
                    return;
                }

                var send = GetWriterPack(channel);
                send.WriteEnqueue(segment, NetworkManager.Time.localTime); // 添加到队列末尾并写入数据

                using var writer = NetworkWriter.Pop();
                if (send.WriteDequeue(writer)) // 尝试从队列中取出元素并写入到目标
                {
                    NetworkManager.Server.OnServerReceive(NetworkConst.HostId, writer, channel);
                }
                else
                {
                    Debug.LogError("客户端不能获取写入器。");
                }
            }
            else
            {
                GetWriterPack(channel).WriteEnqueue(segment, NetworkManager.Time.localTime);
            }
        }

        /// <summary>
        /// 服务器断开连接
        /// </summary>
        public override void Disconnect()
        {
            isReady = false;
            NetworkManager.Client.isReady = false;
            NetworkManager.Transport.ClientDisconnect();
        }
    }
}