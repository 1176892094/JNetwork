using System;
using JFramework.Interface;
using Sirenix.OdinInspector;
using UnityEngine;

// ReSharper disable All

namespace JFramework.Net
{
    public sealed partial class NetworkManager : MonoBehaviour, IEntity
    {
        /// <summary>
        /// NetworkManager 单例
        /// </summary>
        public static NetworkManager Instance;

        /// <summary>
        /// 网络传输组件
        /// </summary>
        [SerializeField] private Transport transport;

        /// <summary>
        /// 网络发现组件
        /// </summary>
        [SerializeField] private NetworkDiscovery discovery;

        /// <summary>
        /// 玩家预置体
        /// </summary>
        [SerializeField] private GameObject player;

        /// <summary>
        /// 网络时间
        /// </summary>
        [Inject, SerializeField] private TimeManager time;

        /// <summary>
        /// 是否进行调试
        /// </summary>
        [Inject, SerializeField] private DebugManager debug;

        /// <summary>
        /// 网络客户端
        /// </summary>
        [Inject, SerializeField] private ClientManager client;

        /// <summary>
        /// 网络服务器
        /// </summary>
        [Inject, SerializeField] private ServerManager server;

        /// <summary>
        /// 网络场景
        /// </summary>
        [Inject, SerializeField] private SceneManager scene;

        /// <summary>
        /// 网络设置
        /// </summary>
        [Inject, SerializeField] private SettingManager setting;

        /// <summary>
        /// 心跳传输率
        /// </summary>
        [SerializeField] internal int tickRate = 30;

        /// <summary>
        /// 客户端最大连接数量
        /// </summary>
        [SerializeField] internal uint connection = 100;

        /// <summary>
        /// 是否进行调试
        /// </summary>
        [SerializeField] private bool isDebug = true;

        /// <summary>
        /// 消息发送率
        /// </summary>
        internal float sendRate => tickRate < int.MaxValue ? 1f / tickRate : 0;

        /// <summary>
        /// 网络运行模式
        /// </summary>
        [ShowInInspector]
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
        public static TimeManager Time => Instance.time;

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
        /// TimerManager 控制器
        /// </summary>
        public static Transport Transport
        {
            get => Instance.transport;
            set => Instance.transport = value;
        }

        /// <summary>
        /// ServerManager 控制器
        /// </summary>
        public static NetworkDiscovery Discovery => Instance.discovery;

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
            Application.runInBackground = true;
#if UNITY_SERVER
            Application.targetFrameRate = tickRate;
#endif
        }

        /// <summary>
        /// 进行更新
        /// </summary>
        private void OnGUI()
        {
            if (isDebug)
            {
                debug.OnUpdate();
            }
        }

        /// <summary>
        /// 启用
        /// </summary>
        private void OnEnable()
        {
            GlobalManager.OnQuit += OnQuit;
        }

        /// <summary>
        /// 禁用
        /// </summary>
        private void OnDisable()
        {
            GlobalManager.OnQuit -= OnQuit;
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
        public void StartClient(Uri uri = default)
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
                Server.OnServerDisconnected(NetworkConst.HostId);
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
        }

        /// <summary>
        /// 生成玩家预置体
        /// </summary>
        /// <param name="client"></param>
        internal void SpawnPrefab(NetworkClient client)
        {
            if (client.isSpawn && player != null)
            {
                Server.Spawn(Instantiate(player), client);
                client.isSpawn = false;
            }
        }
    }
}