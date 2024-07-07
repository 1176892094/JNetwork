using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace JFramework.Udp
{
    public sealed class Server
    {
        private readonly Dictionary<int, Client> clients = new Dictionary<int, Client>();
        private readonly HashSet<int> copies = new HashSet<int>();
        private Socket socket;
        private EndPoint endPoint;
        private readonly byte[] buffer;
        private readonly Setting setting;
        private event Action<int> OnConnect;
        private event Action<int> OnDisconnect;
        private event Action<int, ArraySegment<byte>, int> OnReceive;

        public Server(Setting setting, Action<int> OnConnect, Action<int> OnDisconnect, Action<int, ArraySegment<byte>, int> OnReceive)
        {
            this.setting = setting;
            this.OnReceive = OnReceive;
            this.OnConnect = OnConnect;
            this.OnDisconnect = OnDisconnect;
            buffer = new byte[setting.MaxUnit];
            endPoint = setting.DualMode ? new IPEndPoint(IPAddress.IPv6Any, 0) : new IPEndPoint(IPAddress.Any, 0);
        }

        public void Connect(ushort port)
        {
            if (socket != null)
            {
                Log.Warn("服务器已经连接！");
                return;
            }

            if (setting.DualMode)
            {
                socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                try
                {
                    socket.DualMode = true;
                }
                catch (NotSupportedException e)
                {
                    Log.Warn($"服务器不能设置成双模式！\n{e}");
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    const uint IOC_IN = 0x80000000U;
                    const uint IOC_VENDOR = 0x18000000U;
                    const int SIO_UDP_RESET = unchecked((int)(IOC_IN | IOC_VENDOR | 12));
                    socket.IOControl(SIO_UDP_RESET, new byte[] { 0x00 }, null);
                }

                socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            }
            else
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }

            Common.SetBuffer(socket);
        }

        private bool TryReceive(out ArraySegment<byte> segment, out int clientId)
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
                if (e.SocketErrorCode == SocketError.WouldBlock)
                {
                    return false;
                }

                Log.Error($"服务器接收信息失败！\n{e}");
                return false;
            }
        }


        public void Send(int clientId, ArraySegment<byte> segment, int channel)
        {
            if (clients.TryGetValue(clientId, out Client client))
            {
                client.SendData(segment, channel);
            }
        }

        public void Disconnect(int clientId)
        {
            if (clients.TryGetValue(clientId, out Client client))
            {
                client.Disconnect();
            }
        }

        private Client AddClient(int clientId)
        {
            return new Client(OnConnect, OnDisconnect, OnReceive, OnSend, setting, Common.GenerateCookie(), endPoint);

            void OnConnect(Client client)
            {
                clients.Add(clientId, client);
                Log.Info($"客户端 {clientId} 连接到服务器。");
                this.OnConnect?.Invoke(clientId);
            }

            void OnDisconnect()
            {
                copies.Add(clientId);
                Log.Info($"客户端 {clientId} 从服务器断开。");
                this.OnDisconnect?.Invoke(clientId);
            }

            void OnReceive(ArraySegment<byte> message, int channel)
            {
                this.OnReceive?.Invoke(clientId, message, channel);
            }

            void OnSend(ArraySegment<byte> segment)
            {
                try
                {
                    if (!clients.TryGetValue(clientId, out var client))
                    {
                        Log.Warn($"服务器向无效的客户端发送信息。客户端：{clientId}");
                        return;
                    }

                    if (socket.Poll(0, SelectMode.SelectWrite))
                    {
                        socket.SendTo(segment.Array, segment.Offset, segment.Count, SocketFlags.None, client.endPoint);
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock)
                    {
                        return;
                    }

                    Log.Error($"服务器发送消息失败！\n{e}");
                }
            }
        }

        public void EarlyUpdate()
        {
            while (TryReceive(out var segment, out int clientId))
            {
                if (!clients.TryGetValue(clientId, out var client))
                {
                    client = AddClient(clientId);
                    client.Input(segment);
                    client.EarlyUpdate();
                }

                else
                {
                    client.Input(segment);
                }
            }

            foreach (var client in clients.Values)
            {
                client.EarlyUpdate();
            }

            foreach (int client in copies)
            {
                clients.Remove(client);
            }

            copies.Clear();
        }


        public void AfterUpdate()
        {
            foreach (var client in clients.Values)
            {
                client.AfterUpdate();
            }
        }

        public void StopServer()
        {
            clients.Clear();
            socket?.Close();
            socket = null;
        }

        private sealed class Client : Proxy
        {
            public readonly EndPoint endPoint;
            private event Action OnDisconnect;
            private event Action<Client> OnConnect;
            private event Action<ArraySegment<byte>> OnSend;
            private event Action<ArraySegment<byte>, int> OnReceive;

            public Client(Action<Client> OnConnect, Action OnDisconnect, Action<ArraySegment<byte>, int> OnReceive, Action<ArraySegment<byte>> OnSend, Setting setting, uint cookie, EndPoint endPoint) : base(setting, cookie)
            {
                this.OnSend = OnSend;
                this.OnConnect = OnConnect;
                this.OnReceive = OnReceive;
                this.OnDisconnect = OnDisconnect;
                this.endPoint = endPoint;
                state = State.Connect;
            }

            protected override void Connected()
            {
                SendReliable(ReliableHeader.Connect, default);
                OnConnect?.Invoke(this);
            }

            protected override void Disconnected() => OnDisconnect?.Invoke();

            protected override void Send(ArraySegment<byte> segment) => OnSend?.Invoke(segment);

            protected override void Receive(ArraySegment<byte> message, int channel) => OnReceive?.Invoke(message, channel);

            public void Input(ArraySegment<byte> segment)
            {
                if (segment.Count <= 1 + 4)
                {
                    return;
                }

                var channel = segment.Array[segment.Offset];
                Utility.Decode32U(segment.Array, segment.Offset + 1, out var newCookie);

                if (state == State.Connected)
                {
                    if (newCookie != cookie)
                    {
                        Log.Info($"从 {endPoint} 删除无效cookie: {newCookie}预期:{cookie}。");
                        return;
                    }
                }

                Input(channel, new ArraySegment<byte>(segment.Array, segment.Offset + 1 + 4, segment.Count - 1 - 4));
            }
        }
    }
}