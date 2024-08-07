// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-04  22:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using JFramework.Core;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    public partial class NetworkManager : MonoBehaviour, IEntity
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
        /// 时间组件
        /// </summary>
        [SerializeField, Inject] private TimeManager time;

        /// <summary>
        /// 客户端组件
        /// </summary>
        [SerializeField, Inject] private ClientManager client;

        /// <summary>
        /// 服务器组件
        /// </summary>
        [SerializeField, Inject] private ServerManager server;

        /// <summary>
        /// 场景加载组件
        /// </summary>
        [SerializeField, Inject] private SceneManager scene;

        /// <summary>
        /// 调试器组件
        /// </summary>
        [SerializeField, Inject] private DebugManager debug;

        /// <summary>
        /// 玩家游戏预置体
        /// </summary>
        [SerializeField] private GameObject prefab;

        /// <summary>
        /// 心跳传输率
        /// </summary>
        [SerializeField, Range(30, 120)] internal int sendRate = 30;

        /// <summary>
        /// 客户端最大连接数量
        /// </summary>
        public int connection = 100;

        /// <summary>
        /// 是否进行调试
        /// </summary>
        [SerializeField] private DebugMode debugger = DebugMode.Enable;

        /// <summary>
        /// 流逝时间
        /// </summary>
        internal static double TickTime => UnityEngine.Time.unscaledTimeAsDouble;

        /// <summary>
        /// 消息发送率
        /// </summary>
        internal static float SendRate => 1f / Instance.sendRate;

        /// <summary>
        /// 时间组件
        /// </summary>
        public static TimeManager Time => Instance.time;

        /// <summary>
        /// 客户端组件
        /// </summary>
        public static ClientManager Client => Instance.client;

        /// <summary>
        /// 服务器组件
        /// </summary>
        public static ServerManager Server => Instance.server;

        /// <summary>
        /// 场景加载组件
        /// </summary>
        public static SceneManager Scene => Instance.scene;

        /// <summary>
        /// 网络发现组件
        /// </summary>
        public static NetworkDiscovery Discovery => Instance.discovery;

        /// <summary>
        /// 网络传输组件
        /// </summary>
        public static Transport Transport
        {
            get => Instance.transport;
            set => Instance.transport = value;
        }

        /// <summary>
        /// 游戏模式
        /// </summary>
        public static EntryMode Mode
        {
            get
            {
                if (!Application.isPlaying)
                {
                    return EntryMode.None;
                }

                if (Server.isActive)
                {
                    return Client.isActive ? EntryMode.Host : EntryMode.Server;
                }

                return Client.isActive ? EntryMode.Client : EntryMode.None;
            }
        }

        /// <summary>
        /// 当开启服务器
        /// </summary>
        public event Action OnStartServer;
        
        /// <summary>
        /// 当开启客户端
        /// </summary>
        public event Action OnStartClient;
        
        /// <summary>
        /// 当停止服务器
        /// </summary>
        public event Action OnStopServer;
        
        /// <summary>
        /// 当停止客户端
        /// </summary>
        public event Action OnStopClient;

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            this.Inject();
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
#if UNITY_SERVER
            Application.targetFrameRate = sendRate;
#endif
        }

        /// <summary>
        /// 销毁
        /// </summary>
        private void OnDestroy()
        {
            this.Destroy();
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
        /// 进行更新
        /// </summary>
        private void OnGUI()
        {
            if (debugger != DebugMode.Disable)
            {
                debug.Update();
            }
        }

        /// <summary>
        /// 当程序退出，停止服务器和客户端
        /// </summary>
        private void OnQuit()
        {
            if (Client.isConnected)
            {
                StopClient();
            }

            if (Server.isActive)
            {
                StopServer();
            }
        }
    }

    public partial class NetworkManager
    {
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

            OnStartServer?.Invoke();
            Server.StartServer(EntryMode.Server);
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

            OnStopServer?.Invoke();
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

            OnStartClient?.Invoke();
            Client.StartClient(EntryMode.Client);
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="uri"></param>
        public void StartClient(Uri uri)
        {
            if (Client.isActive)
            {
                Debug.LogWarning("客户端已经连接！");
                return;
            }

            OnStartClient?.Invoke();
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

            if (Mode == EntryMode.Host)
            {
                Server.OnServerDisconnect(Const.HostId);
            }

            OnStopClient?.Invoke();
            Client.StopClient();
        }

        /// <summary>
        /// 开启主机
        /// </summary>
        public void StartHost(EntryMode mode = EntryMode.Host)
        {
            if (Server.isActive || Client.isActive)
            {
                Debug.LogWarning("客户端或服务器已经连接！");
                return;
            }

            OnStartServer?.Invoke();
            Server.StartServer(mode);
            OnStartClient?.Invoke();
            Client.StartClient(EntryMode.Host);
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
        /// 生成玩家预置体
        /// </summary>
        /// <param name="client"></param>
        internal void SpawnPlayer(NetworkClient client)
        {
            if (prefab != null && client.isPlayer)
            {
                Server.Spawn(Instantiate(prefab), client);
                client.isPlayer = false;
            }
        }
    }
}