using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable All

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        /// <summary>
        /// 网络消息委托字典
        /// </summary>
        internal static readonly Dictionary<ushort, MessageDelegate> messages = new Dictionary<ushort, MessageDelegate>();

        /// <summary>
        /// 注册的预置体
        /// </summary>
        internal static readonly Dictionary<uint, GameObject> prefabs = new Dictionary<uint, GameObject>();

        /// <summary>
        /// 场景中包含的网络对象
        /// </summary>
        internal static readonly Dictionary<ulong, NetworkObject> scenes = new Dictionary<ulong, NetworkObject>();

        /// <summary>
        /// 客户端生成的网络对象
        /// </summary>
        internal static readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();

        /// <summary>
        /// 上一次发送信息的时间
        /// </summary>
        private static double lastSendTime;

        /// <summary>
        /// 是否在生成物体中
        /// </summary>
        private static bool isSpawn;

        /// <summary>
        /// 连接的状态
        /// </summary>
        private static ConnectState state;

        /// <summary>
        /// 是否活跃
        /// </summary>
        public static bool isActive => state is ConnectState.Connected or ConnectState.Connecting;

        /// <summary>
        /// 是否已经连接成功
        /// </summary>
        public static bool isConnect => state == ConnectState.Connected;

        /// <summary>
        /// 是否已经准备完成(能进行和Server的信息传输)
        /// </summary>
        public static bool isReady { get; internal set; }

        /// <summary>
        /// 是否正在加载场景
        /// </summary>
        public static bool isLoadScene { get; internal set; }

        /// <summary>
        /// 连接到的服务器
        /// </summary>
        public static ServerEntity connection { get; private set; }

        /// <summary>
        /// 客户端连接的事件(包含主机)
        /// </summary>
        public static event Action OnClientConnect;

        /// <summary>
        /// 客户端断开的事件
        /// </summary>
        public static event Action OnClientDisconnect;

        /// <summary>
        /// 客户端取消准备的事件
        /// </summary>
        public static event Action OnClientNotReady;

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="address">传入连接地址</param>
        /// <param name="port">传入连接端口</param>
        internal static void StartClient(string address, ushort port)
        {
            RegisterTransport();
            RegisterMessage(false);
            state = ConnectState.Connecting;
            Transport.current.ClientConnect(address, port);
            connection = new ServerEntity();
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="uri">传入Uri</param>
        internal static void StartClient(Uri uri)
        {
            RegisterTransport();
            RegisterMessage(false);
            state = ConnectState.Connecting;
            Transport.current.ClientConnect(uri);
            connection = new ServerEntity();
        }

        /// <summary>
        /// 开启主机，使用Server的Transport
        /// </summary>
        internal static void StartClient()
        {
            RegisterMessage(true);
            state = ConnectState.Connected;
            connection = new ServerEntity();
            var client = new ClientEntity(NetworkConst.HostId);
            NetworkServer.OnClientConnect(client);
            Ready();
        }

        /// <summary>
        /// 设置客户端准备(能够进行消息传输)
        /// </summary>
        public static void Ready()
        {
            if (connection == null)
            {
                Debug.LogError("没有连接到有效的服务器！");
                return;
            }
            
            if (isReady)
            {
                Debug.LogError("客户端已经准备就绪！");
                return;
            }
            
            Debug.Log($"客户端准备。");
            isReady = true;
            connection.isReady = true;
            connection.SendMessage(new SetReadyMessage());
        }

        /// <summary>
        /// 客户端发送消息到服务器 (对发送消息的封装)
        /// </summary>
        /// <param name="message">网络事件</param>
        /// <param name="channel">传输通道</param>
        /// <typeparam name="T"></typeparam>
        internal static void SendMessage<T>(T message, Channel channel = Channel.Reliable) where T : struct, Message
        {
            if (connection == null)
            {
                Debug.LogError("没有连接到有效的服务器！");
                return;
            }
            
            if (state != ConnectState.Connected)
            {
                Debug.LogError("客户端没有连接成功就向服务器发送消息！");
                return;
            }

            connection.SendMessage(message, channel);
        }

        /// <summary>
        /// 清除事件
        /// </summary>
        internal static void ClearEvent()
        {
            OnClientConnect = null;
            OnClientDisconnect = null;
            OnClientNotReady = null;
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        internal static void StopClient()
        {
            if (!isActive) return;
            Debug.Log("停止客户端。");
            if (Transport.current != null)
            {
                Transport.current.ClientDisconnect();
            }

            if (NetworkManager.mode is NetworkMode.Host)
            {
                OnClientDisconnect?.Invoke();
            }

            DestroyForClient();
            state = ConnectState.Disconnected;
            lastSendTime = 0;
            scenes.Clear();
            messages.Clear();
            prefabs.Clear();
            isReady = false;
            connection = null;
            isLoadScene = false;
        }
    }
}