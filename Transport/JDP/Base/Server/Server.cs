using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace JFramework.Udp
{
    [Serializable]
    public sealed class Server
    {
        private readonly Dictionary<int, Connection> clients = new Dictionary<int, Connection>();
        private readonly HashSet<int> removes = new HashSet<int>();
        private Socket socket;
        private EndPoint endPoint;
        private readonly byte[] buffer;
        private readonly Setting setting;
        private event Action<int> onConnected;
        private event Action<int> onDisconnected;
        private event Action<int, ArraySegment<byte>, Channel> onReceive;

        public Server(Setting setting, Action<int> onConnected, Action<int> onDisconnected, Action<int, ArraySegment<byte>, Channel> onReceive)
        {
            this.setting = setting;
            this.onConnected = onConnected;
            this.onDisconnected = onDisconnected;
            this.onReceive = onReceive;
            buffer = new byte[setting.maxTransferUnit];
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
            }

            socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            socket.Blocking = false;
            socket.SetBufferSize(setting.sendBufferSize, setting.receiveBufferSize);
        }

        /// <summary>
        /// 服务器断开客户端连接
        /// </summary>
        /// <param name="clientId">断开的客户端Id</param>
        public void Disconnect(int clientId)
        {
            if (clients.TryGetValue(clientId, out var connection))
            {
                connection.peer.Disconnect();
            }
        }

        /// <summary>
        /// 服务器发送消息给指定客户端
        /// </summary>
        public void Send(int clientId, ArraySegment<byte> segment, Channel channel)
        {
            if (clients.TryGetValue(clientId, out var connection))
            {
                connection.peer.Send(segment, channel);
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
                if (socket.ReceiveFormClient(buffer, out segment, ref endPoint))
                {
                    clientId = endPoint.GetHashCode();
                    return true;
                }
            }
            catch (SocketException e)
            {
                Log.Error($"服务器接收信息失败！\n{e}");
            }

            return false;
        }

        /// <summary>
        /// 指定客户端连接到服务器
        /// </summary>
        private Connection Connection(int clientId)
        {
            var newConnection = new Connection(endPoint);
            var cookie = Utils.GenerateCookie();
            var peer = new Peer(OnAuthority, OnDisconnected, OnSend, OnReceive, setting, cookie);
            newConnection.peer = peer;
            return newConnection;

            void OnAuthority()
            {
                newConnection.peer.Handshake();
                Log.Info($"客户端 {clientId} 连接到服务器。");
                clients.Add(clientId, newConnection);
                onConnected?.Invoke(clientId);
            }

            void OnDisconnected()
            {
                removes.Add(clientId);
                Log.Info($"客户端 {clientId} 从服务器断开。");
                onDisconnected?.Invoke(clientId);
            }

            void OnSend(ArraySegment<byte> segment)
            {
                if (!clients.TryGetValue(clientId, out var connection))
                {
                    Log.Warn($"服务器向无效的客户端发送信息。客户端：{clientId}");
                    return;
                }

                try
                {
                    socket.SendToClient(segment, connection.endPoint);
                }
                catch (SocketException e)
                {
                    Log.Error($"服务器发送消息失败！\n{e}");
                }
            }

            void OnReceive(ArraySegment<byte> message, Channel channel)
            {
                onReceive?.Invoke(clientId, message, channel);
            }
        }

        /// <summary>
        /// Update之前
        /// </summary>
        public void EarlyUpdate()
        {
            while (TryReceive(out var clientId, out var segment))
            {
                if (!clients.TryGetValue(clientId, out var connection))
                {
                    connection = Connection(clientId);
                    connection.peer.Input(segment);
                    connection.peer.EarlyUpdate();
                }
                else
                {
                    connection.peer.Input(segment);
                }
            }

            foreach (var connection in clients.Values)
            {
                connection.peer.EarlyUpdate();
            }

            foreach (var clientId in removes)
            {
                clients.Remove(clientId);
            }

            removes.Clear();
        }

        /// <summary>
        /// Update之后
        /// </summary>
        public void AfterUpdate()
        {
            foreach (var connection in clients.Values)
            {
                connection.peer.AfterUpdate();
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