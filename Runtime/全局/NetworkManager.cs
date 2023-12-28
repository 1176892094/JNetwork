using System;
using JFramework.Interface;
using Sirenix.OdinInspector;
using UnityEngine;

// ReSharper disable All

namespace JFramework.Net
{
    public sealed partial class NetworkManager : MonoBehaviour, IInject
    {
        /// <summary>
        /// NetworkManager 单例
        /// </summary>
        public static NetworkManager Instance;

        /// <summary>
        /// 网络传输组件
        /// </summary>
        [LabelText("网络传输"), SerializeField] private Transport transport;

        /// <summary>
        /// 网络发现组件
        /// </summary>
        [LabelText("网络发现"), SerializeField] internal NetworkDiscovery discovery;

        /// <summary>
        /// 玩家预置体
        /// </summary>
        [LabelText("玩家实例"), SerializeField] internal GameObject playerPrefab;

        /// <summary>
        /// 网络客户端
        /// </summary>
        [LabelText("客户端"), Inject, SerializeField]
        private ClientManager client;

        /// <summary>
        /// 网络服务器
        /// </summary>
        [LabelText("服务器"), Inject, SerializeField]
        private ServerManager server;
        
        /// <summary>
        /// 网络场景
        /// </summary>
        [LabelText("网络场景"), Inject, SerializeField]
        private SceneManager scene;
        
        /// <summary>
        /// 网络设置
        /// </summary>
        [LabelText("网络配置"), Inject, SerializeField]
        private SettingManager setting;

        /// <summary>
        /// 网络时间
        /// </summary>
        [LabelText("网络时间"), Inject, SerializeField]
        private TimeManager time;

        /// <summary>
        /// 心跳传输率
        /// </summary>
        [LabelText("心跳"), SerializeField] internal int tickRate = 30;

        /// <summary>
        /// 客户端最大连接数量
        /// </summary>
        [LabelText("最大连接"), SerializeField] internal uint maxConnection = 100;

        /// <summary>
        /// 消息发送率
        /// </summary>
        internal float sendRate => tickRate < int.MaxValue ? 1f / tickRate : 0;

        /// <summary>
        /// 传输连接地址
        /// </summary>
        [LabelText("地址"), ShowInInspector]
        public string address
        {
            get => transport ? transport.address : NetworkConst.Address;
            set => transport.address = transport ? value : NetworkConst.Address;
        }

        /// <summary>
        /// 传输连接端口
        /// </summary>
        [LabelText("端口"), ShowInInspector]
        public ushort port
        {
            get => transport ? transport.port : NetworkConst.Port;
            set => transport.port = transport ? value : NetworkConst.Port;
        }

        /// <summary>
        /// 网络运行模式
        /// </summary>
        [LabelText("模式"), ShowInInspector]
        public NetworkMode mode
        {
            get
            {
                if (!Application.isPlaying)
                {
                    return NetworkMode.None;
                }

                if (Server.isActive)
                {
                    return Client.isActive ? NetworkMode.Host : NetworkMode.Server;
                }

                return Client.isActive ? NetworkMode.Client : NetworkMode.None;
            }
        }
        
        /// <summary>
        /// TimerManager 控制器
        /// </summary>
        internal static TimeManager Time => Instance.time;

        /// <summary>
        /// SceneManager 控制器
        /// </summary>
        public static SceneManager Scene => Instance.scene;

        /// <summary>
        /// ClientManager 控制器
        /// </summary>
        public static ClientManager Client => Instance.client;

        /// <summary>
        /// ServerManager 控制器
        /// </summary>
        public static ServerManager Server => Instance.server;
        
        /// <summary>
        /// SettingManager 控制器
        /// </summary>
        internal static SettingManager Setting => Instance.setting;

        /// <summary>
        /// 初始化配置传输
        /// </summary>
        private void Awake()
        {
            this.Inject();
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GlobalManager.OnQuit += OnQuit;
            Application.runInBackground = true;
#if UNITY_SERVER
            Application.targetFrameRate = tickRate;
#endif
            Transport.current = transport;
        }

        /// <summary>
        /// 开启服务器
        /// </summary>
        public void StartServer()
        {
            if (Server.isActive)
            {
                Debug.LogWarning("服务器已经连接！");
                return;
            }

            Scene.sceneName = "";
            Server.StartServer(true);
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void StopServer()
        {
            if (!Server.isActive)
            {
                Debug.LogWarning("服务器已经停止！");
                return;
            }
            
            Server.StopServer();
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        public void StartClient()
        {
            if (Client.isActive)
            {
                Debug.LogWarning("客户端已经连接！");
                return;
            }

            Client.StartClient(address, port);
        }

        /// <summary>
        /// 开启客户端 (根据Uri来连接)
        /// </summary>
        /// <param name="uri">传入Uri</param>
        public void StartClient(Uri uri)
        {
            if (Client.isActive)
            {
                Debug.LogWarning("客户端已经连接！");
                return;
            }

            Client.StartClient(uri);
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        public void StopClient()
        {
            if (!Client.isActive)
            {
                Debug.LogWarning("客户端已经停止！");
                return;
            }

            if (mode == NetworkMode.Host)
            {
                Server.OnServerDisconnected(Server.connection.clientId);
            }

            Client.StopClient();
        }

        /// <summary>
        /// 开启主机
        /// </summary>
        /// <param name="isListen">设置false则为单机模式，不进行网络传输</param>
        public void StartHost(bool isListen = true)
        {
            if (Server.isActive || Client.isActive)
            {
                Debug.LogWarning("客户端或服务器已经连接！");
                return;
            }

            Server.StartServer(isListen);
            Client.StartClient();
        }

        /// <summary>
        /// 停止主机
        /// </summary>
        public void StopHost()
        {
            StopClient();
            StopServer();
        }

        /// <summary>
        /// 当程序退出，停止服务器和客户端
        /// </summary>
        private void OnQuit()
        {
            if (Client.isAuthority)
            {
                StopClient();
            }

            if (Server.isActive)
            {
                StopServer();
            }

            RuntimeInitializeOnLoad();
            GlobalManager.OnQuit -= OnQuit;
        }
    }
}