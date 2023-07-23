namespace JFramework.Net
{
    internal interface Snapshot
    {
        /// <summary>
        /// 本地时间
        /// </summary>
        double localTime { get; }
        
        /// <summary>
        /// 远端时间
        /// </summary>
        double remoteTime { get; }
    }
}