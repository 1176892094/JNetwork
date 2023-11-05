using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace JFramework.Udp
{
    public static class Helper
    {
        internal const int PING_INTERVAL = 1000;
        internal const int QUEUE_DISCONNECTED_THRESHOLD = 10000;
        internal const int METADATA_SIZE = CHANNEL_HEADER_SIZE + COOKIE_HEADER_SIZE;
        private const int CHANNEL_HEADER_SIZE = 1;
        private const int COOKIE_HEADER_SIZE = 4;

        /// <summary>
        /// 编码8位无符号整型
        /// </summary>
        internal static int Encode8u(byte[] p, int offset, byte value)
        {
            p[0 + offset] = value;
            return 1;
        }

        /// <summary>
        /// 解码8位无符号整型
        /// </summary>
        internal static int Decode8u(byte[] p, int offset, out byte value)
        {
            value = p[0 + offset];
            return 1;
        }

        /// <summary>
        /// 编码16位无符号整型
        /// </summary>
        internal static int Encode16U(byte[] p, int offset, ushort value)
        {
            p[0 + offset] = (byte)(value >> 0);
            p[1 + offset] = (byte)(value >> 8);
            return 2;
        }

        /// <summary>
        /// 解码16位无符号整型
        /// </summary>
        internal static int Decode16U(byte[] p, int offset, out ushort value)
        {
            ushort result = 0;
            result |= p[0 + offset];
            result |= (ushort)(p[1 + offset] << 8);
            value = result;
            return 2;
        }

        /// <summary>
        /// 编码32位无符号整型
        /// </summary>
        internal static int Encode32U(byte[] p, int offset, uint value)
        {
            p[0 + offset] = (byte)(value >> 0);
            p[1 + offset] = (byte)(value >> 8);
            p[2 + offset] = (byte)(value >> 16);
            p[3 + offset] = (byte)(value >> 24);
            return 4;
        }

        /// <summary>
        /// 解码32位无符号整型
        /// </summary>
        internal static int Decode32U(byte[] p, int offset, out uint value)
        {
            uint result = 0;
            result |= p[0 + offset];
            result |= (uint)(p[1 + offset] << 8);
            result |= (uint)(p[2 + offset] << 16);
            result |= (uint)(p[3 + offset] << 24);
            value = result;
            return 4;
        }

        /// <summary>
        /// 解析主机地址
        /// </summary>
        internal static bool TryGetAddress(string host, out IPAddress[] addresses)
        {
            try
            {
                addresses = Dns.GetHostAddresses(host);
                return addresses.Length >= 1;
            }
            catch (SocketException exception)
            {
                Log.Error($"无法解析主机地址：{host}\n{exception}");
                addresses = null;
                return false;
            }
        }

        /// <summary>
        /// 生成缓存文件
        /// </summary>
        internal static int GenerateCookie()
        {
            using var cryptoRandom = new RNGCryptoServiceProvider();
            var cryptoRandomBuffer = new byte[4];
            cryptoRandom.GetBytes(cryptoRandomBuffer);
            return Math.Abs(BitConverter.ToInt32(cryptoRandomBuffer, 0));
        }

        /// <summary>
        /// 可靠传输大小(255, 148716)
        /// </summary>
        public static int ReliableSize(int maxTransferUnit, uint receivePacketSize)
        {
            return ReliableSizeInternal(maxTransferUnit, Math.Min(receivePacketSize, Protocol.FRG_MAX));
        }

        /// <summary>
        /// 可靠传输大小(内部) 148716
        /// </summary>
        private static int ReliableSizeInternal(int maxTransferUnit, uint receivePacketSize)
        {
            return (maxTransferUnit - Protocol.OVERHEAD - METADATA_SIZE) * ((int)receivePacketSize - 1) - 1;
        }

        /// <summary>
        /// 不可靠传输大小
        /// </summary>
        public static int UnreliableSize(int maxTransmissionUnit)
        {
            return maxTransmissionUnit - METADATA_SIZE;
        }
    }
}