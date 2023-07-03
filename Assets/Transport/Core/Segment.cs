using System.IO;

namespace Transport
{
    internal class Segment
    {
        /// <summary>
        /// 我们需要一个自动调整大小的 byte[]，带有WriteBytes函数，MemoryStream 正好具备这样的功能
        /// </summary>
        public readonly MemoryStream stream = new MemoryStream(Jdp.MTU_DEF);

        public uint conv; //会话标识
        public uint command;
        public uint fragment; // 分段（以1字节发送）
        public uint windowSize; // 接收方当前可以接收的窗口大小
        public uint timestamp; //时间戳
        public uint serialNumber; //序列号
        public uint unAcknowledge;
        public uint resendTimestamp; //重传时间戳
        public uint fastAcknowledge;
        public uint retransmitCount; //重传计数
        public int retransmitTimeout;
        
        public int Encode(byte[] ptr, int offset)
        {
            int previousPosition = offset;
            offset += Utils.Encode32U(ptr, offset, conv);
            offset += Utils.Encode8u(ptr, offset, (byte)command);
            offset += Utils.Encode8u(ptr, offset, (byte)fragment);
            offset += Utils.Encode16U(ptr, offset, (ushort)windowSize);
            offset += Utils.Encode32U(ptr, offset, timestamp);
            offset += Utils.Encode32U(ptr, offset, serialNumber);
            offset += Utils.Encode32U(ptr, offset, unAcknowledge);
            offset += Utils.Encode32U(ptr, offset, (uint)stream.Position);
            return offset - previousPosition;
        }

        public void Reset()
        {
            conv = 0;
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