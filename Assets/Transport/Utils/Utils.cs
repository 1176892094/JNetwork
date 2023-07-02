using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Transport
{
    public class Utils
    {
        public const int Timeout = 10000;
        public const int PackageSend = 32;
        public const int PackageReceive = 128;
        public const int MaxTransferUnit = 1200;
        public const int MaxFragment = byte.MaxValue;
        public const int Overhead = 24;
        private const int MetaDataSize = ChannelHeaderSize + CookieHeaderSize;
        private const int ChannelHeaderSize = 1;
        private const int CookieHeaderSize = 4;
        
        /// <summary>
        /// 编码8位无符号整型
        /// </summary>
        public static int Encode8u(byte[] p, int offset, byte value)
        {
            p[0 + offset] = value;
            return 1;
        }

        /// <summary>
        /// 解码8位无符号整型
        /// </summary>
        public static int Decode8u(byte[] p, int offset, out byte value)
        {
            value = p[0 + offset];
            return 1;
        }

        /// <summary>
        /// 编码16位无符号整型
        /// </summary>
        public static int Encode16U(byte[] p, int offset, ushort value)
        {
            p[0 + offset] = (byte)(value >> 0);
            p[1 + offset] = (byte)(value >> 8);
            return 2;
        }

        /// <summary>
        /// 解码16位无符号整型
        /// </summary>
        public static int Decode16U(byte[] p, int offset, out ushort value)
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
        public static int Encode32U(byte[] p, int offset, uint value)
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
        
        /// <summary>
        /// 解析主机地址
        /// </summary>
        public static bool TryGetAddress(string host, out IPAddress address)
        {
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(host);
                address = addresses[0];
                return addresses.Length >= 1;
            }
            catch (SocketException exception)
            {
                Log.Error($"Failed to resolve host: {host}\n{exception}");
                address = null;
                return false;
            }
        }

        /// <summary>
        /// 生成缓存文件
        /// </summary>
        public static uint GenerateCookie()
        {
            using var cryptoRandom = new RNGCryptoServiceProvider();
            var cryptoRandomBuffer = new byte[4];
            cryptoRandom.GetBytes(cryptoRandomBuffer);
            return BitConverter.ToUInt32(cryptoRandomBuffer, 0);
        }

        /// <summary>
        /// 可靠传输大小
        /// </summary>
        public static int ReliableSize(int maxTransferUnit, int packageReceive)
        {
            return ReliableSizeInternal(maxTransferUnit, Math.Min(packageReceive, MaxFragment));
        }

        /// <summary>
        /// 可靠传输大小(内部) 148716
        /// </summary>
        private static int ReliableSizeInternal(int maxTransferUnit, int packageReceive)
        {
            return (maxTransferUnit - Overhead - MetaDataSize) * (packageReceive - 1) - 1;
        }

        /// <summary>
        /// 不可靠传输大小
        /// </summary>
        public static int UnreliableSize(int maxTransmissionUnit)
        {
            return maxTransmissionUnit - MetaDataSize;
        }
    }
}