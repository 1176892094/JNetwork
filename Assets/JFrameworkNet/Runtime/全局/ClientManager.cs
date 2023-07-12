using System;
using System.Collections.Generic;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class ClientManager
    {
        /// <summary>
        /// 网络消息委托字典
        /// </summary>
        internal static readonly Dictionary<ushort, EventDelegate> events = new Dictionary<ushort, EventDelegate>();
        
        /// <summary>
        /// 注册的预置体
        /// </summary>
        internal static readonly Dictionary<uint, GameObject> prefabs = new Dictionary<uint, GameObject>();

        /// <summary>
        /// 场景中包含的网络对象
        /// </summary>
        internal static readonly Dictionary<ulong, NetworkObject> scenes = new Dictionary<ulong, NetworkObject>();

        /// <summary>
        /// 客户端生成的物体数量
        /// </summary>
        internal static readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();

        /// <summary>
        /// 连接到的服务器
        /// </summary>
        public static ServerEntity connection;

        /// <summary>
        /// 是否已经准备完成(能进行和Server的信息传输)
        /// </summary>
        public static bool isReady;

        /// <summary>
        /// 是否在生成物体中
        /// </summary>
        private static bool isSpawn;

        /// <summary>
        /// 是否正在加载场景
        /// </summary>
        public static bool isLoadScene;

        /// <summary>
        /// 上一次发送信息的时间
        /// </summary>
        private static double lastSendTime;

        /// <summary>
        /// 连接的状态
        /// </summary>
        private static ConnectState state;

        /// <summary>
        /// 网络消息读取并分包
        /// </summary>
        private static NetworkReaders readers = new NetworkReaders();

        /// <summary>
        /// 是否活跃
        /// </summary>
        public static bool isActive => state is ConnectState.Connected or ConnectState.Connecting;

        /// <summary>
        /// 是否已经连接成功
        /// </summary>
        public static bool isAuthority => state == ConnectState.Connected;

        /// <summary>
        /// 心跳包
        /// </summary>
        private static int tickRate => NetworkManager.Instance.tickRate;

        /// <summary>
        /// 消息发送率
        /// </summary>
        private static float sendRate => tickRate < int.MaxValue ? 1f / tickRate : 0;

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="address">传入连接地址</param>
        /// <param name="port">传入连接端口</param>
        internal static void StartClient(string address, ushort port)
        {
            RegisterTransport();
            RegisterEvent(false);
            readers = new NetworkReaders();
            state = ConnectState.Connecting;
            RegisterPrefab(NetworkManager.prefabs);
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
            RegisterEvent(false);
            readers = new NetworkReaders();
            state = ConnectState.Connecting;
            RegisterPrefab(NetworkManager.prefabs);
            Transport.current.ClientConnect(uri);
            connection = new ServerEntity();
        }

        /// <summary>
        /// 开启主机，使用Server的Transport
        /// </summary>
        internal static void StartClient()
        {
            Debug.Log("开启客户端。");
            RegisterEvent(true);
            readers = new NetworkReaders();
            state = ConnectState.Connected;
            RegisterPrefab(NetworkManager.prefabs);
            connection = new ServerEntity();
            ServerManager.connection = new ClientEntity(NetworkConst.HostId);
        }

        /// <summary>
        /// 设置客户端准备(能够进行消息传输)
        /// </summary>
        public static void Ready()
        {
            if (isReady)
            {
                Debug.LogError("客户端已经准备就绪！");
            }
            else if (connection == null)
            {
                Debug.LogError("没有有效的服务器连接！");
            }
            else
            {
                Debug.Log($"客户端准备。");
                isReady = true;
                connection.isReady = true;
                connection.Send(new ReadyEvent());
            }
        }

        /// <summary>
        /// 客户端断开连接
        /// </summary>
        public static void Disconnect()
        {
            if (state is ConnectState.Disconnecting or ConnectState.Disconnected)
            {
                return;
            }
            
            state = ConnectState.Disconnecting;
            connection?.Disconnect();
        }

        /// <summary>
        /// 可毒案发送消息到服务器
        /// </summary>
        /// <param name="event">网络事件</param>
        /// <param name="channel">传输通道</param>
        /// <typeparam name="T"></typeparam>
        public static void Send<T>(T @event, Channel channel = Channel.Reliable) where T : struct, IEvent
        {
            if (connection != null)
            {
                if (state == ConnectState.Connected)
                {
                    connection.Send(@event, channel);
                }
                else
                {
                    Debug.LogError("客户端没有连接成功就向服务器发送消息！");
                }
            }
            else
            {
                Debug.LogError("没有有效的服务器连接！");
            }
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        public static void StopClient()
        {
            Debug.Log("停止客户端。");
            state = ConnectState.Disconnected;
            if (Transport.current != null)
            {
                Transport.current.ClientDisconnect();
            }

            spawns.Clear();
            events.Clear();
            prefabs.Clear();
            connection = null;
            isReady = false;
            isLoadScene = false;
        }
    }
}