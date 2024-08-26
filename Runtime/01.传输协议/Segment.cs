using System.IO;

namespace JFramework.Udp
{
    internal class Segment
    {
        public readonly MemoryStream data = new MemoryStream(Kcp.MTU_DEF);
        public uint conv;
        public uint cmd;
        public uint frg;
        public uint wnd;
        public uint ts;
        public uint sn;
        public uint una;
        public uint rsd_c;
        public uint rsd_ts;
        public uint fast_ack;
        public int rto;
        
        public int Encode(byte[] ptr, int offset)
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

        public void Reset()
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