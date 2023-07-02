using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class Server
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

        public void Connect(Config config)
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

            socket.Bind(new IPEndPoint(IPAddress.IPv6Any, config.port));
            socket.Blocking = false;
            socket.SendBufferSize = setting.sendBufferSize;
            socket.ReceiveBufferSize = setting.receiveBufferSize;
        }

        public void Disconnect(int clientId)
        {
            if (clients.TryGetValue(clientId, out var connection))
            {
                connection.peer.Disconnect();
            }
        }

        public void Send(int clientId, ArraySegment<byte> segment, Channel channel)
        {
            if (clients.TryGetValue(clientId, out var connection))
            {
                connection.peer.Send(segment, channel);
            }
        }

        public bool Receive(out int clientId, out ArraySegment<byte> segment)
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

        private void SendInternal(int clientId, ArraySegment<byte> data)
        {
            if (!clients.TryGetValue(clientId, out var connection))
            {
                Log.Warn($"The server send invalid clientId = {clientId}");
                return;
            }

            try
            {
                socket.SendToClient(data, connection.endPoint);
            }
            catch (SocketException e)
            {
                Log.Error($"The server send failed.\n{e}");
            }
        }

        private void Connection(int clientId)
        {
            var connection = new Connection(endPoint);
            var cookie = Utils.GenerateCookie();
            var peerData = new PeerData(OnAuthority, OnDisconnected, OnSend, OnReceive);
            var peer = new Peer(peerData, setting, cookie);
            connection.peer = peer;

            void OnAuthority()
            {
                connection.peer.SendHandshake();
                Log.Info($"The client {clientId} connect to server.");
                clients.Add(clientId, connection);
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
                try
                {
                    socket.SendToServer(segment);
                }
                catch (Exception e)
                {
                    Log.Error($"Client send failed!\n{e}");
                }
            }

            void OnReceive(ArraySegment<byte> message)
            {
                serverData.onReceive?.Invoke(clientId, message);
            }
        }
    }
}