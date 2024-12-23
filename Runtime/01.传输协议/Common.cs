// *********************************************************************************
// # Project: JFramework
// # Unity: 6000.3.5f1
// # Author: 云谷千羽
// # Version: 1.0.0
// # History: 2024-11-29 13:11:20
// # Recently: 2024-12-22 20:12:12
// # Copyright: 2024, 云谷千羽
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        private static readonly byte[] cryptoRandomBuffer = new byte[4];

        public static uint GenerateCookie()
        {
            RandomNumberGenerator.Fill(cryptoRandomBuffer);
            return MemoryMarshal.Read<uint>(cryptoRandomBuffer);
        }

        public static int ReliableSize(int mtu, uint rcv_wnd)
        {
            return (mtu - Kcp.OVERHEAD - METADATA_SIZE) * ((int)Math.Min(rcv_wnd, Kcp.FRG_MAX) - 1) - 1;
        }

        public static int UnreliableSize(int mtu)
        {
            return mtu - METADATA_SIZE - 1;
        }

        internal static bool ParseReliable(byte value, out Reliable header)
        {
            if (Enum.IsDefined(typeof(Reliable), value))
            {
                header = (Reliable)value;
                return true;
            }

            header = Reliable.Ping;
            return false;
        }

        internal static bool ParseUnreliable(byte value, out Unreliable header)
        {
            if (Enum.IsDefined(typeof(Unreliable), value))
            {
                header = (Unreliable)value;
                return true;
            }

            header = Unreliable.Disconnect;
            return false;
        }

        internal static void SetBuffer(Socket socket)
        {
            socket.Blocking = false;
            var sendBuffer = socket.SendBufferSize;
            var receiveBuffer = socket.ReceiveBufferSize;
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
}