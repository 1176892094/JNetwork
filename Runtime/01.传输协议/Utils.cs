// *********************************************************************************
// # Project: JFramework
// # Unity: 6000.3.5f1
// # Author: 云谷千羽
// # Version: 1.0.0
// # History: 2024-11-30 17:11:43
// # Recently: 2024-12-22 20:12:07
// # Copyright: 2024, 云谷千羽
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System.Runtime.CompilerServices;

namespace JFramework.Udp
{
    internal static class Utils
    {
        public static int Encode8U(byte[] p, int offset, byte value)
        {
            p[0 + offset] = value;
            return 1;
        }

        public static int Decode8U(byte[] p, int offset, out byte value)
        {
            value = p[0 + offset];
            return 1;
        }

        public static int Encode16U(byte[] p, int offset, ushort value)
        {
            p[0 + offset] = (byte)(value >> 0);
            p[1 + offset] = (byte)(value >> 8);
            return 2;
        }

        public static int Decode16U(byte[] p, int offset, out ushort value)
        {
            ushort result = 0;
            result |= p[0 + offset];
            result |= (ushort)(p[1 + offset] << 8);
            value = result;
            return 2;
        }

        public static int Encode32U(byte[] p, int offset, uint value)
        {
            p[0 + offset] = (byte)(value >> 0);
            p[1 + offset] = (byte)(value >> 8);
            p[2 + offset] = (byte)(value >> 16);
            p[3 + offset] = (byte)(value >> 24);
            return 4;
        }

        public static int Decode32U(byte[] p, int offset, out uint value)
        {
            uint result = 0;
            result |= p[0 + offset];
            result |= (uint)(p[1 + offset] << 8);
            result |= (uint)(p[2 + offset] << 16);
            result |= (uint)(p[3 + offset] << 24);
            value = result;
            return 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compare(uint later, uint earlier)
        {
            return (int)(later - earlier);
        }
    }
}