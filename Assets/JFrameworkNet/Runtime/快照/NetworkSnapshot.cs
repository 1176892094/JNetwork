using System.Collections.Generic;

namespace JFramework.Net
{
    internal static class NetworkSnapshot
    {
        /// <summary>
        /// 快照存储字典
        /// </summary>
        public static readonly SortedList<double, SnapshotTime> snapshots = new SortedList<double, SnapshotTime>();

        /// <summary>
        /// 网络设置
        /// </summary>
        public static SnapshotData snapshotSettings;
        
        /// <summary>
        /// 当前时间量程
        /// </summary>
        private static double localTimescale = 1;
        
        /// <summary>
        /// 当前时间线
        /// </summary>
        internal static double localTimeline;
        
        /// <summary>
        /// 移动指数平均值
        /// </summary>
        private static NetworkEma driftEma;
        
        /// <summary>
        /// 发送时间移动指数平均值
        /// </summary>
        private static NetworkEma deliveryTimeEma;
        
        /// <summary>
        /// 缓存时间
        /// </summary>
        private static double bufferTime => NetworkManager.sendRate * snapshotSettings.bufferTimeMultiplier;
        
        /// <summary>
        /// 初始化时间差值
        /// </summary>
        public static void InitInterpolation()
        {
            snapshots.Clear();
            localTimeline = 0;
            localTimescale = 1;
            driftEma = new NetworkEma(NetworkManager.Instance.tickRate * snapshotSettings.driftEmaDuration);
            deliveryTimeEma = new NetworkEma(NetworkManager.Instance.tickRate  * snapshotSettings.deliveryTimeEmaDuration);
        }

        /// <summary>
        /// 快照处理
        /// </summary>
        /// <param name="snapshot">新的快照</param>
        public static void SnapshotTime(SnapshotTime snapshot)
        {
            if (snapshotSettings.dynamicAdjustment)
            {
                snapshotSettings.bufferTimeMultiplier = SnapshotUtils.DynamicAdjust(NetworkManager.sendRate, deliveryTimeEma.StandardDeviation, snapshotSettings.dynamicAdjustmentTolerance);
            }
            
            SnapshotUtils.InsertAndAdjust(snapshots, snapshot, ref localTimeline, ref localTimescale, NetworkManager.sendRate, bufferTime, ref driftEma, ref deliveryTimeEma);
        }
    }
}