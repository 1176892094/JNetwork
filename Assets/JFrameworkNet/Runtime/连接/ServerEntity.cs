using System;
using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 仅在主机或者客户端模式下创建
    /// </summary>
    public class ServerEntity : Connection
    {
        /// <summary>
        /// 存储写入队列的字典
        /// </summary>
        internal readonly Queue<NetworkWriter> writeQueue = new Queue<NetworkWriter>();

        /// <summary>
        /// 网络消息读取并分包
        /// </summary>
        internal readonly NetworkReaderPack readers = new NetworkReaderPack();

        /// <summary>
        /// 网络设置
        /// </summary>
        private readonly NetworkSetting settingData;
        
        /// <summary>
        /// 当前时间线
        /// </summary>
        private double localTimeline;

        /// <summary>
        /// 当前时间量程
        /// </summary>
        private double localTimescale = 1;

        /// <summary>
        /// 缓存时间
        /// </summary>
        private double bufferTime => NetworkManager.sendRate * settingData.bufferTimeMultiplier;

        /// <summary>
        /// 构造函数初始化
        /// </summary>
        public ServerEntity()
        {
            settingData = NetworkManager.Instance.setting;
            driftEma = new NetworkEma(NetworkManager.Instance.tickRate * settingData.driftEmaDuration);
            deliveryTimeEma = new NetworkEma(NetworkManager.Instance.tickRate  * settingData.deliveryTimeEmaDuration);
        }

        /// <summary>
        /// 快照处理
        /// </summary>
        /// <param name="snapshot">新的快照</param>
        internal void OnSnapshotMessage(SnapshotTime snapshot)
        {
            if (settingData.dynamicAdjustment)
            {
                settingData.bufferTimeMultiplier = SnapshotUtils.DynamicAdjust(NetworkManager.sendRate, deliveryTimeEma.deviation, settingData.dynamicAdjustmentTolerance);
            }
            
            SnapshotUtils.InsertAndAdjust(snapshots, snapshot, ref localTimeline, ref localTimescale, NetworkManager.sendRate, bufferTime, ref driftEma, ref deliveryTimeEma);
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
            if (NetworkManager.mode != NetworkMode.Host) return;
            while (writeQueue.Count > 0)
            {
                using var writer = writeQueue.Dequeue(); // 从队列中取出
                var segment = writer.ToArraySegment(); //转化成数据分段
                var writers = GetWriterPack(Channel.Reliable); // 获取可靠传输
                writers.WriteEnqueue(segment, NetworkTime.localTime); // 将数据写入到队列
                using var template = NetworkWriter.Pop(); // 取出新的 writer
                if (writers.WriteDequeue(template)) // 将 writer 拷贝到 template
                {
                    NetworkClient.OnClientReceive(template.ToArraySegment(), Channel.Reliable);
                }
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="channel">传输通道</param>
        internal override void SendMessage(ArraySegment<byte> segment, Channel channel = Channel.Reliable)
        {
            if (NetworkManager.mode == NetworkMode.Host)
            {
                if (segment.Count == 0)
                {
                    Debug.LogError("发送消息大小不能为零！");
                    return;
                }

                var send = GetWriterPack(channel);
                send.WriteEnqueue(segment, NetworkTime.localTime); // 添加到队列末尾并写入数据

                using var writer = NetworkWriter.Pop();
                if (send.WriteDequeue(writer)) // 尝试从队列中取出元素并写入到目标
                {
                    NetworkServer.OnServerReceive(NetworkConst.HostId, writer.ToArraySegment(), channel);
                }
                else
                {
                    Debug.LogError("客户端不能获取写入器。");
                }
            }
            else
            {
                GetWriterPack(channel).WriteEnqueue(segment, NetworkTime.localTime);
            }
        }

        /// <summary>
        /// 服务器断开连接
        /// </summary>
        public override void Disconnect()
        {
            isReady = false;
            NetworkClient.isReady = false;
            Transport.current.ClientDisconnect();
        }
    }
}