namespace JFramework.Net
{
    public struct NetworkConst
    {
        /// <summary>
        /// 最大网络行为数量
        /// </summary>
        public const int MaxNetworkBehaviours = 64;
        
        /// <summary>
        /// 客户端连接Id
        /// </summary>
        public const int ConnectionId = 0;
        
        /// <summary>
        /// 头部大小
        /// </summary>
        public const int HeaderSize = sizeof(double);
        
        /// <summary>
        /// 网络消息大小
        /// </summary>
        public const int IdSize = sizeof(ushort);
    }
}