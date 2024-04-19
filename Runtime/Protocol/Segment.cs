using System.IO;

namespace JFramework.Udp
{
    /// <summary>
    /// 网络消息分段
    /// </summary>
    internal sealed class Segment
    {
        /// <summary>
        /// 内存流
        /// </summary>
        public readonly MemoryStream stream = new MemoryStream(Protocol.MTU_DEF);

        /// <summary>
        /// 会话Id
        /// </summary>
        public uint id;
        
        /// <summary>
        /// 超时重传
        /// </summary>
        public int rto;

        /// <summary>
        /// 命令
        /// </summary>
        public uint command;

        /// <summary>
        /// 分段（以1字节发送）
        /// </summary>
        public uint fragment;

        /// <summary>
        /// 接收方当前可以接收的窗口大小
        /// </summary>
        public uint window;

        /// <summary>
        /// 序列号
        /// </summary>
        public uint sendId;
        
        /// <summary>
        /// 时间戳
        /// </summary>
        public uint sendTime;

        /// <summary>
        /// 快速重传的序列号
        /// </summary>
        public uint resendId;
        
        /// <summary>
        /// 重传时间戳
        /// </summary>
        public uint resendTime;

        /// <summary>
        /// 重传计数
        /// </summary>
        public uint resendCount;
        
        /// <summary>
        /// 未确认的序列号
        /// </summary>
        public uint receiveId;

        /// <summary>
        /// 编码
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public int Encode(byte[] ptr, int offset)
        {
            int previous = offset;
            offset += Utility.Encode32U(ptr, offset, id);
            offset += Utility.Encode8U(ptr, offset, (byte)command);
            offset += Utility.Encode8U(ptr, offset, (byte)fragment);
            offset += Utility.Encode16U(ptr, offset, (ushort)window);
            offset += Utility.Encode32U(ptr, offset, sendTime);
            offset += Utility.Encode32U(ptr, offset, sendId);
            offset += Utility.Encode32U(ptr, offset, receiveId);
            offset += Utility.Encode32U(ptr, offset, (uint)stream.Position);
            return offset - previous;
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            id = 0;
            rto = 0;
            command = 0;
            fragment = 0;
            window = 0;
            sendId = 0;
            sendTime = 0;
            receiveId = 0;
            resendId = 0;
            resendTime = 0;
            resendCount = 0;
            stream.SetLength(0);
        }
    }
}