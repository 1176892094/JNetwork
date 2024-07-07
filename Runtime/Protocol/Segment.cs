using System.IO;

namespace JFramework.Udp
{
    internal class Segment
    {
        internal readonly MemoryStream data = new MemoryStream(Protocol.MTU_DEF);
        internal uint una;
        internal uint conv;
        internal uint command;
        internal uint fragment;
        internal uint sendId;
        internal uint fastAck;
        internal uint sendTime;
        internal uint windowSize;
        internal uint resendTime;
        internal uint resendCount;
        internal int resendTimeout;
        
        internal int Encode(byte[] ptr, int offset)
        {
            int position = offset;
            offset += Utility.Encode32U(ptr, offset, conv);
            offset += Utility.Encode8U(ptr, offset, (byte)command);
            offset += Utility.Encode8U(ptr, offset, (byte)fragment);
            offset += Utility.Encode16U(ptr, offset, (ushort)windowSize);
            offset += Utility.Encode32U(ptr, offset, sendTime);
            offset += Utility.Encode32U(ptr, offset, sendId);
            offset += Utility.Encode32U(ptr, offset, una);
            offset += Utility.Encode32U(ptr, offset, (uint)data.Position);
            return offset - position;
        }


        internal void Reset()
        {
            una = 0;
            conv = 0;
            sendId = 0;
            command = 0;
            fastAck = 0;
            fragment = 0;
            sendTime = 0;
            windowSize = 0;
            resendTime = 0;
            resendCount = 0;
            resendTimeout = 0;
            data.SetLength(0);
        }
    }
}