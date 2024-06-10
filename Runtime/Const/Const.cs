// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  04:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

namespace JFramework.Net
{
    internal static class Const
    {
        /// <summary>
        /// Ping的间隔
        /// </summary>
        public const float PingInterval = 2;

        /// <summary>
        /// Ping窗口
        /// </summary>
        public const int PingWindow = 6;
        
        /// <summary>
        /// 最大网络行为数量
        /// </summary>
        public const int MaxEntityCount = 64;

        /// <summary>
        /// 客户端连接Id
        /// </summary>
        public const int HostId = 0;

        /// <summary>
        /// 网络消息的字符串最大长度
        /// </summary>
        public const ushort MaxStringLength = ushort.MaxValue - 1;

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