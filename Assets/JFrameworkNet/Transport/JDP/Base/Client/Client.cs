using System;
using System.Net;
using System.Net.Sockets;

namespace JFramework.Udp
{
    [Serializable]
    public sealed class Client
    {
        private State state;
        private Peer peer;
        private Socket socket;
        private EndPoint endPoint;
        private readonly byte[] buffer;
        private readonly Setting setting;
        private readonly Action onConnected;
        private readonly Action onDisconnected;
        private readonly Action<ArraySegment<byte>, Channel> onReceive;

        public Client(Setting setting, Action onConnected, Action onDisconnected, Action<ArraySegment<byte>, Channel> onReceive)
        {
            this.setting = setting;
            this.onConnected = onConnected;
            this.onDisconnected = onDisconnected;
            this.onReceive = onReceive;
            buffer = new byte[setting.maxTransferUnit];
            state = State.Disconnected;
        }

        /// <summary>
        /// 连接到指定服务器
        /// </summary>
        /// <param name="address"></param>
        public void Connect(Address address)
        {
            if (state == State.Connected)
            {
                Log.Info("Client is already connected");
                return;
            }

            if (!Utils.TryGetAddress(address.ip, out var addresses))
            {
                onDisconnected?.Invoke();
                return;
            }

            Connection();
            endPoint = new IPEndPoint(addresses[0], address.port);
            Log.Info($"Client connect to {addresses[0]} : {address.port}");
            socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Blocking = false;
            socket.SendBufferSize = setting.sendBufferSize;
            socket.ReceiveBufferSize = setting.receiveBufferSize;
            socket.Connect(endPoint);
            peer.SendHandshake();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (state == State.Disconnected)
            {
                return;
            }
            
            peer?.Disconnect();
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="segment">字节消息数组</param>
        /// <param name="channel">传输通道</param>
        public void Send(ArraySegment<byte> segment, Channel channel)
        {
            if (state == State.Disconnected)
            {
                Log.Warn($"Client send failed!");
                return;
            }

            peer.Send(segment, channel);
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="segment">字节消息数组</param>
        private bool TryReceive(out ArraySegment<byte> segment)
        {
            segment = default;
            if (socket == null) return false;
            try
            {
                return socket.ReceiveFormServer(buffer, out segment);
            }
            catch (SocketException e)
            {
                Log.Info($"Client receive failed!\n{e}");
                peer.Disconnect();
                return false;
            }
        }

        /// <summary>
        /// 创建Peer
        /// </summary>
        private void Connection()
        {
            peer = new Peer(OnAuthority, OnDisconnected, OnSend, onReceive, setting, 0);

            void OnAuthority()
            {
                Log.Info("Client connected.");
                state = State.Connected;
                onConnected?.Invoke();
            }

            void OnDisconnected()
            {
                Log.Info($"Client disconnected");
                socket.Close();
                peer = null;
                socket = null;
                endPoint = null;
                state = State.Disconnected;
                onDisconnected?.Invoke();
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
        }
        
        /// <summary>
        /// Update之前
        /// </summary>
        public void EarlyUpdate()
        {
            if (peer != null)
            {
                while (TryReceive(out var segment))
                {
                    peer.Input(segment);
                }
            }

            peer?.EarlyUpdate();
        }

        /// <summary>
        /// Update之后
        /// </summary>
        public void AfterUpdate()
        {
            peer?.AfterUpdate();
        }
    }
}