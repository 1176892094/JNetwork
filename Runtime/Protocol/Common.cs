using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace JFramework.Udp
{
    public static class Common
    {
        internal const int BUFFER_DEF = 1024 * 1027 * 7;
        internal const int PING_INTERVAL = 1000;
        
        internal const int CHANNEL_HEADER_SIZE = 1;
        internal const int COOKIE_HEADER_SIZE = 4;
        internal const int METADATA_SIZE = CHANNEL_HEADER_SIZE + COOKIE_HEADER_SIZE;

        private static readonly RNGCryptoServiceProvider cryptoRandom = new RNGCryptoServiceProvider();
        private static readonly byte[] cryptoRandomBuffer = new byte[4];

        internal static uint GenerateCookie()
        {
            cryptoRandom.GetBytes(cryptoRandomBuffer);
            return BitConverter.ToUInt32(cryptoRandomBuffer, 0);
        }

        public static int ReliableSize(int mtu, uint rcv_wnd)
        {
            return (mtu - Protocol.OVERHEAD - METADATA_SIZE) * ((int)Math.Min(rcv_wnd, Protocol.FRG_MAX) - 1) - 1;
        }

        public static int UnreliableSize(int mtu)
        {
            return mtu - METADATA_SIZE - 1;
        }

        internal static bool ParseReliable(byte value, out ReliableHeader header)
        {
            if (Enum.IsDefined(typeof(ReliableHeader), value))
            {
                header = (ReliableHeader)value;
                return true;
            }

            header = ReliableHeader.Ping;
            return false;
        }

        internal static bool ParseUnreliable(byte value, out UnreliableHeader header)
        {
            if (Enum.IsDefined(typeof(UnreliableHeader), value))
            {
                header = (UnreliableHeader)value;
                return true;
            }

            header = UnreliableHeader.Disconnect;
            return false;
        }

        internal static void SetBuffer(Socket socket)
        {
            socket.Blocking = false;
            int sendBuffer = socket.SendBufferSize;
            int receiveBuffer = socket.ReceiveBufferSize;
            try
            {
                socket.SendBufferSize = BUFFER_DEF;
                socket.ReceiveBufferSize = BUFFER_DEF;
            }
            catch (SocketException)
            {
                Log.Info($"发送缓存: {BUFFER_DEF} => {sendBuffer} : {sendBuffer / BUFFER_DEF:F}");
                Log.Info($"接收缓存: {BUFFER_DEF} => {receiveBuffer} : {receiveBuffer / BUFFER_DEF:F}");
            }
        }
    }

    internal static class Utility
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