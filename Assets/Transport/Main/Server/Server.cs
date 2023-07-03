using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace JDP
{
    public sealed class Server
    {
        private readonly Dictionary<int, Connection> clients = new Dictionary<int, Connection>();
        private readonly HashSet<int> removes = new HashSet<int>();
        private Socket socket;
        private EndPoint endPoint;
        private readonly byte[] buffer;
        private readonly Setting setting;
        private readonly ServerData serverData;

        public Server(Setting setting, ServerData serverData)
        {
            this.setting = setting;
            this.serverData = serverData;
            buffer = new byte[setting.maxTransferUnit];
            endPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
        }

        /// <summary>
        /// 服务器启动
        /// </summary>
        /// <param name="address">配置地址和端口号</param>
        public void Connect(Address address)
        {
            if (socket != null)
            {
                Log.Info("Server is already connected");
                return;
            }

            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                socket.DualMode = true;
            }
            catch (Exception e)
            {
                Log.Warn($"Server failed to set Dual Mode.\n{e}");
            }

            socket.Bind(new IPEndPoint(IPAddress.IPv6Any, address.port));
            socket.Blocking = false;
            socket.SendBufferSize = setting.sendBufferSize;
            socket.ReceiveBufferSize = setting.receiveBufferSize;
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
            catch (Exception e)
            {
                Log.Error($"Server receive failed!\n{e}");
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
            var peerData = new PeerData(OnAuthority, OnDisconnected, OnSend, OnReceive);
            var peer = new Peer(peerData, setting, cookie);
            newConnection.peer = peer;
            return newConnection;

            void OnAuthority()
            {
                newConnection.peer.SendHandshake();
                Log.Info($"The client {clientId} connect to server.");
                clients.Add(clientId, newConnection);
                serverData.onConnected(clientId);
            }

            void OnDisconnected()
            {
                removes.Add(clientId);
                Log.Info($"The client {clientId} disconnect to server.");
                serverData.onDisconnected?.Invoke(clientId);
            }

            void OnSend(ArraySegment<byte> segment)
            {
                if (!clients.TryGetValue(clientId, out var connection))
                {
                    Log.Warn($"The server send invalid clientId = {clientId}");
                    return;
                }

                try
                {
                    socket.SendToClient(segment, connection.endPoint);
                }
                catch (SocketException e)
                {
                    Log.Error($"The server send failed.\n{e}");
                }
            }

            void OnReceive(ArraySegment<byte> message, Channel channel)
            {
                serverData.onReceive?.Invoke(clientId, message, channel);
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
        public void ShutDown()
        {
            socket?.Close();
            socket = null;
        }
    }
}