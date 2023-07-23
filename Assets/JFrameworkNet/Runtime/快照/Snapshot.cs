namespace JFramework.Net
{
    internal interface ISnapshot
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