namespace JFramework.Net
{
   internal static class Compression
    {
        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public static void CompressVarUInt(NetworkWriter writer, ulong value)
        {
            if (value <= 240)
            {
                writer.WriteByte((byte)value);
                return;
            }
            if (value <= 2287)
            {
                writer.WriteByte((byte)(((value - 240) >> 8) + 241));
                writer.WriteByte((byte)((value - 240) & 0xFF));
                return;
            }
            if (value <= 67823)
            {
                writer.WriteByte(249);
                writer.WriteByte((byte)((value - 2288) >> 8));
                writer.WriteByte((byte)((value - 2288) & 0xFF));
                return;
            }
            if (value <= 16777215)
            {
                writer.WriteByte(250);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                return;
            }
            if (value <= 4294967295)
            {
                writer.WriteByte(251);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                writer.WriteByte((byte)((value >> 24) & 0xFF));
                return;
            }
            if (value <= 1099511627775)
            {
                writer.WriteByte(252);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                writer.WriteByte((byte)((value >> 24) & 0xFF));
                writer.WriteByte((byte)((value >> 32) & 0xFF));
                return;
            }
            if (value <= 281474976710655)
            {
                writer.WriteByte(253);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                writer.WriteByte((byte)((value >> 24) & 0xFF));
                writer.WriteByte((byte)((value >> 32) & 0xFF));
                writer.WriteByte((byte)((value >> 40) & 0xFF));
                return;
            }
            if (value <= 72057594037927935)
            {
                writer.WriteByte(254);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                writer.WriteByte((byte)((value >> 24) & 0xFF));
                writer.WriteByte((byte)((value >> 32) & 0xFF));
                writer.WriteByte((byte)((value >> 40) & 0xFF));
                writer.WriteByte((byte)((value >> 48) & 0xFF));
            }
            else
            {
                writer.WriteByte(255);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                writer.WriteByte((byte)((value >> 24) & 0xFF));
                writer.WriteByte((byte)((value >> 32) & 0xFF));
                writer.WriteByte((byte)((value >> 40) & 0xFF));
                writer.WriteByte((byte)((value >> 48) & 0xFF));
                writer.WriteByte((byte)((value >> 56) & 0xFF));
            }
        }
   
        /// <summary>
        /// 解压
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ulong DecompressVarUInt(NetworkReader reader)
        {
            byte a0 = reader.ReadByte();
            if (a0 < 241)
            {
                return a0;
            }

            byte a1 = reader.ReadByte();
            if (a0 <= 248)
            {
                return 240 + ((a0 - (ulong)241) << 8) + a1;
            }

            byte a2 = reader.ReadByte();
            if (a0 == 249)
            {
                return 2288 + ((ulong)a1 << 8) + a2;
            }

            byte a3 = reader.ReadByte();
            if (a0 == 250)
            {
                return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16);
            }

            byte a4 = reader.ReadByte();
            if (a0 == 251)
            {
                return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16) + ((ulong)a4 << 24);
            }

            byte a5 = reader.ReadByte();
            if (a0 == 252)
            {
                return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16) + ((ulong)a4 << 24) + ((ulong)a5 << 32);
            }

            byte a6 = reader.ReadByte();
            if (a0 == 253)
            {
                return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16) + ((ulong)a4 << 24) + ((ulong)a5 << 32) + ((ulong)a6 << 40);
            }

            byte a7 = reader.ReadByte();
            if (a0 == 254)
            {
                return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16) + ((ulong)a4 << 24) + ((ulong)a5 << 32) + ((ulong)a6 << 40) + ((ulong)a7 << 48);
            }

            byte a8 = reader.ReadByte();
            return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16) + ((ulong)a4 << 24) + ((ulong)a5 << 32) + ((ulong)a6 << 40) + ((ulong)a7 << 48)  + ((ulong)a8 << 56);
        }
    }
}