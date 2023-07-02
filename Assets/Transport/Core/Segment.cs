using System.IO;

namespace Transport
{
    internal class Segment
    {
        public readonly MemoryStream data = new MemoryStream(Utils.MaxTransferUnit);
        
        public uint identity;        // 对话ID，用于标识通信的对话或连接
        public uint command;         // 数据包的命令类型，例如Kcp.CMD_ACK等
        public uint fragment;        // 分片序号，用于指示数据包是否被分片以及所属分片的序号
        public uint package;         // 接收窗口大小，表示接收端当前可以接收的数据包数量
        public uint timestamp;       // 时间戳，用于记录数据包发送或接收的时间
        public uint sequenceId;      // 序列号，用于对数据包进行排序和确认处理
        public uint unknownId;       // 未确认的序列号，表示接收端已收到但尚未确认的最大序列号
        public uint resendTimeTrip;  // 重传时间戳，用于记录数据包的重传时间
        public uint fastAcknowledge; // 快速确认计数，表示接收端已接收到的连续、无缺失的数据包数量
        public uint resendCount;     // 重传次数，表示数据包的重传次数
        public int resendTimeout;    // 超时重传时间（Round-Trip Time Out），表示数据包的超时时间
        
        public int Encode(byte[] ptr, int offset)
        {
            int previousPosition = offset;
            offset += Utils.Encode32U(ptr, offset, identity);
            offset += Utils.Encode8u(ptr, offset, (byte)command);
            offset += Utils.Encode8u(ptr, offset, (byte)fragment);
            offset += Utils.Encode16U(ptr, offset, (ushort)package);
            offset += Utils.Encode32U(ptr, offset, timestamp);
            offset += Utils.Encode32U(ptr, offset, sequenceId);
            offset += Utils.Encode32U(ptr, offset, unknownId);
            offset += Utils.Encode32U(ptr, offset, (uint)data.Position);
            int write = offset - previousPosition;
            return write;
        }
        
        public void Reset()
        {
            identity = 0;
            command = 0;
            fragment = 0;
            package = 0;
            timestamp = 0;
            sequenceId = 0;
            unknownId = 0;
            resendCount = 0;
            resendTimeTrip = 0;
            fastAcknowledge = 0;
            resendTimeout = 0;
            data.SetLength(0);
        }
    }
}