using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager : MonoBehaviour
    {
        /// <summary>
        /// NetworkManager 单例
        /// </summary>
        public static NetworkManager Instance;

        /// <summary>
        /// NetworkClient 控制器
        /// </summary>
        public static NetworkClient Client;

        /// <summary>
        /// NetworkServer 控制器
        /// </summary>
        public static NetworkServer Server;

        /// <summary>
        /// 服务器场景
        /// </summary>
        private string sceneName;

        /// <summary>
        /// 消息发送率
        /// </summary>
        internal float sendRate => tickRate < int.MaxValue ? 1f / tickRate : 0;

        /// <summary>
        /// 网络传输组件
        /// </summary>
        [SerializeField] private Transport transport;

        /// <summary>
        /// 网络发现组件
        /// </summary>
        [SerializeField] public NetworkDiscovery discovery;

        /// <summary>
        /// 网络设置和预置体
        /// </summary>
        [SerializeField] internal NetworkSetting setting = new NetworkSetting();

        /// <summary>
        /// 玩家预置体
        /// </summary>
        [SerializeField] internal GameObject playerPrefab;

        /// <summary>
        /// 心跳传输率
        /// </summary>
        [SerializeField] internal int tickRate = 30;

        /// <summary>
        /// 客户端最大连接数量
        /// </summary>
        [SerializeField] internal uint maxConnection = 100;

        /// <summary>
        /// 传输连接地址
        /// </summary>
        [ShowInInspector]
        public string address
        {
            get => transport ? transport.address : NetworkConst.Address;
            set => transport.address = transport ? value : NetworkConst.Address;
        }

        /// <summary>
        /// 传输连接端口
        /// </summary>
        [ShowInInspector]
        public ushort port
        {
            get => transport ? transport.port : NetworkConst.Port;
            set => transport.port = transport ? value : NetworkConst.Port;
        }

        /// <summary>
        /// 网络运行模式
        /// </summary>
        [ShowInInspector]
        public NetworkMode mode
        {
            get
            {
                if (Server.isActive)
                {
                    return Client.isActive ? NetworkMode.Host : NetworkMode.Server;
                }

                return Client.isActive ? NetworkMode.Client : NetworkMode.None;
            }
        }

        /// <summary>
        /// 初始化配置传输
        /// </summary>
        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GlobalManager.OnQuit += OnQuit;

            if (transport == null)
            {
                Debug.LogError("NetworkManager 没有 Transport 组件。");
                return;
            }

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

            sceneName = "";
            Server.StartServer(true);
            OnStartServer?.Invoke();
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

            sceneName = "";
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


            Client.StartClient(address, port);
            OnStartClient?.Invoke();
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
            OnStartClient?.Invoke();
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

            OnStopClient?.Invoke();
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
            OnStartHost?.Invoke();
        }

        /// <summary>
        /// 停止主机
        /// </summary>
        public void StopHost()
        {
            OnStopHost?.Invoke();
            StopClient();
            StopServer();
        }

        /// <summary>
        /// 生成玩家预置体
        /// </summary>
        /// <param name="client"></param>
        private void SpawnPrefab(UnityClient client)
        {
            if (client.isSpawn && playerPrefab != null)
            {
                Server.Spawn(Instantiate(playerPrefab), client);
                client.isSpawn = false;
            }
        }

        /// <summary>
        /// 客户端 Ping
        /// </summary>
        /// <param name="ping"></param>
        internal void ClientPingUpdate(double ping)
        {
            OnClientPingUpdate?.Invoke(ping);
        }

        /// <summary>
        /// 当程序退出，停止服务器和客户端
        /// </summary>
        private void OnQuit()
        {
            if (Client.isConnect)
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