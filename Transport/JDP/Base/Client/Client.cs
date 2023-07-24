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
        private event Action onConnected;
        private event Action onDisconnected;
        private event Action<ArraySegment<byte>, Channel> onReceive;

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
        /// <param name="port"></param>
        public void Connect(string address, ushort port)
        {
            if (state == State.Connected)
            {
                Log.Warn("客户端已经连接！");
                return;
            }

            if (!Utils.TryGetAddress(address, out var addresses))
            {
                onDisconnected?.Invoke();
                return;
            }

            Connection();
            endPoint = new IPEndPoint(addresses[0], port);
            Log.Info($"客户端连接到：{addresses[0]} 端口：{port}。");
            socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Blocking = false;
            socket.SendBufferSize = setting.sendBufferSize;
            socket.ReceiveBufferSize = setting.receiveBufferSize;
            socket.Connect(endPoint);
            peer.Handshake();
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
                Log.Warn("客户端没有连接，发送消息失败！");
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
                Log.Info($"客户端接收消息失败！\n{e}");
                peer?.Disconnect();
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
                Log.Info("客户端连接成功。");
                state = State.Connected;
                onConnected?.Invoke();
            }

            void OnDisconnected()
            {
                Log.Info($"客户端断开连接。");
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
                    Log.Error($"客户端发送消息失败！\n{e}");
                }
            }
        }

        /// <summary>
        /// Update之前
        /// </summary>
        public void EarlyUpdate()
        {
            if (peer == null)
            {
                return;
            }

            while (TryReceive(out var segment))
            {
                peer.Input(segment);
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