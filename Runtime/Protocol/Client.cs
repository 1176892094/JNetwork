using System;
using System.Net;
using System.Net.Sockets;

namespace JFramework.Udp
{
    /// <summary>
    /// Udp客户端
    /// </summary>
    public sealed class Client
    {
        /// <summary>
        /// 客户端状态
        /// </summary>
        private State state;

        /// <summary>
        /// 代理
        /// </summary>
        private Proxy proxy;

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
        /// 当客户端连接到服务器
        /// </summary>
        private event Action OnConnect;

        /// <summary>
        /// 当客户端从服务器断开
        /// </summary>
        private event Action OnDisconnect;

        /// <summary>
        /// 当从服务器接收消息
        /// </summary>
        private event Action<ArraySegment<byte>, int> OnReceive;

        /// <summary>
        /// 构造函数初始化
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="OnConnect"></param>
        /// <param name="OnDisconnect"></param>
        /// <param name="OnReceive"></param>
        public Client(Setting setting, Action OnConnect, Action OnDisconnect, Action<ArraySegment<byte>, int> OnReceive)
        {
            this.setting = setting;
            this.OnReceive = OnReceive;
            this.OnConnect = OnConnect;
            this.OnDisconnect = OnDisconnect;
            buffer = new byte[setting.unit];
            state = State.Disconnect;
        }

        /// <summary>
        /// 连接到指定服务器
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public void Connect(string address, ushort port)
        {
            if (state == State.Connect)
            {
                Log.Warn("客户端已经连接！");
                return;
            }

            if (!Utility.TryGetAddress(address, out var addresses))
            {
                OnDisconnect?.Invoke();
                return;
            }

            SetProxy();
            endPoint = new IPEndPoint(addresses[0], port);
            Log.Info($"客户端连接到：{addresses[0]} 端口：{port}。");
            socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            Utility.SetBuffer(socket);
            socket.Connect(endPoint);
            proxy.Connect();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (state == State.Disconnect)
            {
                return;
            }

            proxy?.Disconnect();
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="segment">字节消息数组</param>
        /// <param name="channel">传输通道</param>
        public void Send(ArraySegment<byte> segment, int channel)
        {
            if (state == State.Disconnect)
            {
                Log.Warn("客户端没有连接，发送消息失败！");
                return;
            }

            proxy.Send(segment, channel);
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
                if (!socket.Poll(0, SelectMode.SelectRead)) return false;
                int size = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                segment = new ArraySegment<byte>(buffer, 0, size);
                return true;
            }
            catch (SocketException e)
            {
                Log.Info($"客户端接收消息失败！\n{e}");
                proxy?.Disconnect();
                return false;
            }
        }

        /// <summary>
        /// 创建代理
        /// </summary>
        private void SetProxy()
        {
            proxy = new Proxy(setting, 0, OnConnect, OnDisconnect, OnSend, OnReceive);

            void OnConnect()
            {
                Log.Info("客户端连接成功。");
                state = State.Connect;
                this.OnConnect?.Invoke();
            }

            void OnDisconnect()
            {
                Log.Info($"客户端断开连接。");
                socket.Close();
                proxy = null;
                socket = null;
                endPoint = null;
                state = State.Disconnect;
                this.OnDisconnect?.Invoke();
            }

            void OnSend(ArraySegment<byte> segment)
            {
                try
                {
                    if (socket.Poll(0, SelectMode.SelectWrite))
                    {
                        socket.Send(segment.Array, segment.Offset, segment.Count, SocketFlags.None);
                    }
                }
                catch (SocketException e)
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
            if (proxy == null)
            {
                return;
            }

            while (TryReceive(out var segment))
            {
                proxy.Input(segment);
            }

            proxy?.EarlyUpdate();
        }

        /// <summary>
        /// Update之后
        /// </summary>
        public void AfterUpdate()
        {
            proxy?.AfterUpdate();
        }
    }
}