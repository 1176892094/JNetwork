using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace JFramework.Udp
{
    /// <summary>
    /// Udp服务器
    /// </summary>
    public sealed class Server
    {
        /// <summary>
        /// 连接客户端字典
        /// </summary>
        private readonly Dictionary<int, Proxies> clients = new Dictionary<int, Proxies>();

        /// <summary>
        /// 移除客户端列表
        /// </summary>
        private readonly HashSet<int> copies = new HashSet<int>();

        /// <summary>
        /// 套接字
        /// </summary>
        private Socket socket;

        /// <summary>
        /// 终端
        /// </summary>
        private EndPoint endPoint;

        /// <summary>
        /// 缓冲区
        /// </summary>
        private readonly byte[] buffer;

        /// <summary>
        /// 配置
        /// </summary>
        private readonly Setting setting;

        /// <summary>
        /// 当有客户端连接到服务器
        /// </summary>
        private event Action<int> OnConnect;

        /// <summary>
        /// 当有客户端从服务器断开
        /// </summary>
        private event Action<int> OnDisconnect;

        /// <summary>
        /// 当从客户端收到消息
        /// </summary>
        private event Action<int, ArraySegment<byte>, int> OnReceive;

        /// <summary>
        /// 构造函数初始化
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="OnConnect"></param>
        /// <param name="OnDisconnect"></param>
        /// <param name="OnReceive"></param>
        public Server(Setting setting, Action<int> OnConnect, Action<int> OnDisconnect, Action<int, ArraySegment<byte>, int> OnReceive)
        {
            this.setting = setting;
            this.OnReceive = OnReceive;
            this.OnConnect = OnConnect;
            this.OnDisconnect = OnDisconnect;
            buffer = new byte[setting.unit];
            endPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
        }

        /// <summary>
        /// 服务器启动
        /// </summary>
        /// <param name="port">配置端口号</param>
        public void Connect(ushort port)
        {
            if (socket != null)
            {
                Log.Warn("服务器已经连接！");
                return;
            }

            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                socket.DualMode = true;
            }
            catch (Exception e)
            {
                Log.Warn($"服务器不能设置成双模式！\n{e}");
                socket.DualMode = false;
            }

            socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            Utility.SetBuffer(socket);
        }

        /// <summary>
        /// 服务器断开客户端连接
        /// </summary>
        /// <param name="clientId">断开的客户端Id</param>
        public void Disconnect(int clientId)
        {
            if (clients.TryGetValue(clientId, out var client))
            {
                client.proxy.Disconnect();
            }
        }

        /// <summary>
        /// 服务器发送消息给指定客户端
        /// </summary>
        public void Send(int clientId, ArraySegment<byte> segment, int channel)
        {
            if (clients.TryGetValue(clientId, out var client))
            {
                client.proxy.Send(segment, channel);
            }
        }

        /// <summary>
        /// 服务器从指定客户端接收消息
        /// </summary>
        private bool TryReceive(out int clientId, out ArraySegment<byte> segment)
        {
            clientId = 0;
            segment = default;
            if (socket == null) return false;
            try
            {
                if (!socket.Poll(0, SelectMode.SelectRead)) return false;
                int size = socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endPoint);
                segment = new ArraySegment<byte>(buffer, 0, size);
                clientId = endPoint.GetHashCode();
                return true;
            }
            catch (SocketException e)
            {
                Log.Error($"服务器接收信息失败！\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 指定客户端连接到服务器
        /// </summary>
        private Proxies SetProxy(int clientId)
        {
            var client = new Proxies(endPoint);
            var proxy = new Proxy(setting, Utility.Cookie(), OnConnect, OnDisconnect, OnSend, OnReceive);
            client.proxy = proxy;
            return client;

            void OnConnect()
            {
                client.proxy.Connect();
                Log.Info($"客户端 {clientId} 连接到服务器。");
                clients.Add(clientId, client);
                this.OnConnect?.Invoke(clientId);
            }

            void OnDisconnect()
            {
                copies.Add(clientId);
                Log.Info($"客户端 {clientId} 从服务器断开。");
                this.OnDisconnect?.Invoke(clientId);
            }

            void OnSend(ArraySegment<byte> segment)
            {
                try
                {
                    if (!clients.TryGetValue(clientId, out var connection))
                    {
                        Log.Warn($"服务器向无效的客户端发送信息。客户端：{clientId}");
                        return;
                    }

                    if (socket.Poll(0, SelectMode.SelectWrite))
                    {
                        socket.SendTo(segment.Array, segment.Offset, segment.Count, SocketFlags.None, connection.endPoint);
                    }
                }
                catch (SocketException e)
                {
                    Log.Error($"服务器发送消息失败！\n{e}");
                }
            }

            void OnReceive(ArraySegment<byte> message, int channel)
            {
                this.OnReceive?.Invoke(clientId, message, channel);
            }
        }

        /// <summary>
        /// Update之前
        /// </summary>
        public void EarlyUpdate()
        {
            while (TryReceive(out var clientId, out var segment))
            {
                if (!clients.TryGetValue(clientId, out var client))
                {
                    client = SetProxy(clientId);
                    client.proxy.Input(segment);
                    client.proxy.EarlyUpdate();
                }
                else
                {
                    client.proxy.Input(segment);
                }
            }

            foreach (var client in clients.Values)
            {
                client.proxy.EarlyUpdate();
            }

            foreach (var clientId in copies)
            {
                clients.Remove(clientId);
            }

            copies.Clear();
        }

        /// <summary>
        /// Update之后
        /// </summary>
        public void AfterUpdate()
        {
            foreach (var client in clients.Values)
            {
                client.proxy.AfterUpdate();
            }
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void StopServer()
        {
            socket?.Close();
            socket = null;
        }
    }
}