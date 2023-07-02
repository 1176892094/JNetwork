using System;
using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public static class Extension
    {
        /// <summary>
        /// 用于在非阻塞模式下将数据发送到指定的EndPoint
        /// </summary>
        public static bool SendToNonBlocking(this Socket socket, ArraySegment<byte> data, EndPoint endPoint)
        {
            try
            {
                if (!socket.Poll(0, SelectMode.SelectWrite))
                {
                    return false;
                }

                if (data.Array != null)
                {
                    socket.SendTo(data.Array, data.Offset, data.Count, SocketFlags.None, endPoint);
                }

                return true;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock) return false;
                throw;
            }
        }

        /// <summary>
        /// 用于在非阻塞模式下将数据发送到已连接的EndPoint
        /// </summary>
        public static bool SendNonBlocking(this Socket socket, ArraySegment<byte> data)
        {
            try
            {
                if (!socket.Poll(0, SelectMode.SelectWrite))
                {
                    return false;
                }

                if (data.Array != null)
                {
                    socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
                }

                return true;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock) return false;
                throw;
            }
        }

        /// <summary>
        /// 用于在非阻塞模式下从指定的EndPoint接收数据
        /// </summary>
        public static bool ReceiveFromNonBlocking(this Socket socket, byte[] receiveBuffer, out ArraySegment<byte> data, ref EndPoint endPoint)
        {
            data = default;
            try
            {
                if (!socket.Poll(0, SelectMode.SelectRead))
                {
                    return false;
                }

                int size = socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref endPoint);
                data = new ArraySegment<byte>(receiveBuffer, 0, size);
                return true;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock) return false;
                throw;
            }
        }

        /// <summary>
        /// 用于在非阻塞模式下从已连接的EndPoint接收数据
        /// </summary>
        public static bool ReceiveNonBlocking(this Socket socket, byte[] receiveBuffer, out ArraySegment<byte> data)
        {
            data = default;
            try
            {
                if (!socket.Poll(0, SelectMode.SelectRead))
                {
                    return false;
                }

                int size = socket.Receive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None);
                data = new ArraySegment<byte>(receiveBuffer, 0, size);
                return true;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock) return false;
                throw;
            }
        }
    }
}