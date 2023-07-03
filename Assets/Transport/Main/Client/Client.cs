using System;
using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public sealed class Client
    {
        private State state;
        private Peer peer;
        private Socket socket;
        private EndPoint endPoint;
        private readonly byte[] buffer;
        private readonly Setting setting;
        private readonly ClientData clientData;

        public Client(Setting setting, ClientData clientData)
        {
            this.setting = setting;
            this.clientData = clientData;
            buffer = new byte[setting.maxTransferUnit];
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

            if (!Utils.TryGetAddress(address.ip, out var ip))
            {
                clientData.onDisconnected?.Invoke();
                return;
            }

            Connection();
            endPoint = new IPEndPoint(ip, address.port);
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

            peer.Disconnect();
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
                Log.Error($"Client send failed!");
                return;
            }

            peer.Send(segment, channel);
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="segment">字节消息数组</param>
        public bool Receive(out ArraySegment<byte> segment)
        {
            segment = default;
            if (socket == null) return false;
            try
            {
                socket.ReceiveFormServer(buffer, out segment);
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Client receive failed!\n{e}");
                peer.Disconnect();
                return false;
            }
        }

        /// <summary>
        /// 创建Peer
        /// </summary>
        private void Connection()
        {
            var peerData = new PeerData(OnAuthority, OnDisconnected, OnSend, clientData.onReceive);
            peer = new Peer(peerData, setting, 0);

            void OnAuthority()
            {
                Log.Info("Client connected.");
                state = State.Connected;
                clientData.onConnected?.Invoke();
            }

            void OnDisconnected()
            {
                Log.Info($"Client disconnected");
                socket.Close();
                peer = null;
                socket = null;
                endPoint = null;
                state = State.Disconnected;
                clientData.onDisconnected?.Invoke();
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
        public void BeforeUpdate()
        {
            if (peer != null)
            {
                while (Receive(out var segment))
                {
                    //TODO:客户端操作相关
                    peer.Send(segment, Channel.Reliable);
                }
            }

            peer?.BeforeUpdate();
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