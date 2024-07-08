using System.IO;

namespace JFramework.Udp
{
    internal class Segment
    {
        internal readonly MemoryStream data = new MemoryStream(Protocol.MTU_DEF);
        internal uint conv;
        internal uint cmd;
        internal uint frg;
        internal uint wnd;
        internal uint ts;
        internal uint sn;
        internal uint una;
        internal uint rsd_c;
        internal uint rsd_ts;
        internal uint fast_ack;
        internal int rto;
        
        internal int Encode(byte[] ptr, int offset)
        {
            int position = offset;
            offset += Utility.Encode32U(ptr, offset, conv);
            offset += Utility.Encode8U(ptr, offset, (byte)cmd);
            offset += Utility.Encode8U(ptr, offset, (byte)frg);
            offset += Utility.Encode16U(ptr, offset, (ushort)wnd);
            offset += Utility.Encode32U(ptr, offset, ts);
            offset += Utility.Encode32U(ptr, offset, sn);
            offset += Utility.Encode32U(ptr, offset, una);
            offset += Utility.Encode32U(ptr, offset, (uint)data.Position);
            return offset - position;
        }

        internal void Reset()
        {
            conv = 0;
            cmd = 0;
            frg = 0;
            wnd = 0;
            una = 0;
            ts = 0;
            sn = 0;
            rsd_c = 0;
            rsd_ts = 0;
            fast_ack = 0;
            rto = 0;
            data.SetLength(0);
        }
    }
}