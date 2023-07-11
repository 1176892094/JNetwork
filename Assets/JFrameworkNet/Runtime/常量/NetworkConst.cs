namespace JFramework.Net
{
    internal struct NetworkConst
    {
        /// <summary>
        /// 网络消息的字符串最大长度
        /// </summary>
        public const ushort MaxStringLength = ushort.MaxValue - 1;
        
        /// <summary>
        /// 最大网络行为数量
        /// </summary>
        public const int MaxEntityCount = 64;
        
        /// <summary>
        /// 客户端连接Id
        /// </summary>
        public const int HostId = 0;
        
        /// <summary>
        /// 头部大小
        /// </summary>
        public const int HeaderSize = sizeof(double);
        
        /// <summary>
        /// 网络消息大小
        /// </summary>
        public const int MessageSize = sizeof(ushort);
    }
}