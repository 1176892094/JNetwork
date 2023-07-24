namespace JFramework.Net
{
    internal struct SnapshotTime : Snapshot
    {
        /// <summary>
        /// 本地时间
        /// </summary>
        public double localTime { get; }

        /// <summary>
        /// 远端时间
        /// </summary>
        public double remoteTime { get; }

        /// <summary>
        /// 快照初始化
        /// </summary>
        /// <param name="remoteTime">远端时间</param>
        /// <param name="localTime">本地时间</param>
        public SnapshotTime(double remoteTime, double localTime)
        {
            this.localTime = localTime;
            this.remoteTime = remoteTime;
        }
    }
}