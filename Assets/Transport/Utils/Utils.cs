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
        private const int Overhead = 24;
        private const int MaxFragment = byte.MaxValue;
        private const int MetaDataSize = ChannelHeaderSize + CookieHeaderSize;
        private const int ChannelHeaderSize = 1;
        private const int CookieHeaderSize = 4;
       

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
                Log.Info($"Failed to resolve host: {host}\n{exception}");
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