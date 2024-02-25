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
        public uint conversation;

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
        public uint windowSize;

        /// <summary>
        /// 时间戳
        /// </summary>
        public uint timestamp;

        /// <summary>
        /// 序列号
        /// </summary>
        public uint serialNumber;

        /// <summary>
        /// 未确认的序列号
        /// </summary>
        public uint unAcknowledge;

        /// <summary>
        /// 重传时间戳
        /// </summary>
        public uint resendTimestamp;

        /// <summary>
        /// 快速重传的序列号
        /// </summary>
        public uint fastAcknowledge;

        /// <summary>
        /// 重传计数
        /// </summary>
        public uint retransmitCount;

        /// <summary>
        /// 超时重传
        /// </summary>
        public int retransmitTimeout;

        /// <summary>
        /// 编码
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public int Encode(byte[] ptr, int offset)
        {
            int previousPosition = offset;
            offset += Helper.Encode32U(ptr, offset, conversation);
            offset += Helper.Encode8u(ptr, offset, (byte)command);
            offset += Helper.Encode8u(ptr, offset, (byte)fragment);
            offset += Helper.Encode16U(ptr, offset, (ushort)windowSize);
            offset += Helper.Encode32U(ptr, offset, timestamp);
            offset += Helper.Encode32U(ptr, offset, serialNumber);
            offset += Helper.Encode32U(ptr, offset, unAcknowledge);
            offset += Helper.Encode32U(ptr, offset, (uint)stream.Position);
            return offset - previousPosition;
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            conversation = 0;
            command = 0;
            fragment = 0;
            windowSize = 0;
            timestamp = 0;
            serialNumber = 0;
            unAcknowledge = 0;
            retransmitTimeout = 0;
            retransmitCount = 0;
            resendTimestamp = 0;
            fastAcknowledge = 0;
            stream.SetLength(0);
        }
    }
}