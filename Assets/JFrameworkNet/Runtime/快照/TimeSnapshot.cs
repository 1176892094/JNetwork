namespace JFramework.Net
{
    public struct TimeSnapshot
    {
        /// <summary>
        /// 本地时间
        /// </summary>
        public double localTime;

        /// <summary>
        /// 远端时间
        /// </summary>
        public double remoteTime;

        /// <summary>
        /// 快照初始化
        /// </summary>
        /// <param name="remoteTime">远端时间</param>
        /// <param name="localTime">本地时间</param>
        public TimeSnapshot(double remoteTime, double localTime)
        {
            this.localTime = localTime;
            this.remoteTime = remoteTime;
        }
    }
}