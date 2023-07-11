using System;
using Sirenix.OdinInspector;
using UnityEngine;

// ReSharper disable All

namespace JFramework.Net
{
    public sealed partial class NetworkManager : GlobalSingleton<NetworkManager>
    {
        private string sceneName;
        private NetworkMode networkMode;
        [SerializeField] private Transport transport;
        public uint tickRate = 30;
        public uint maxConnection = 100;

        /// <summary>
        /// 设置地址
        /// </summary>
        [ShowInInspector]
        public string Address
        {
            get => transport ? transport.address : "localhost";
            set
            {
                if (transport)
                {
                    transport.address = value;
                }
                else
                {
                    Debug.LogWarning("The NetworkManager has no Transport component");
                }
            }
        }

        /// <summary>
        /// 设置端口
        /// </summary>
        [ShowInInspector]
        public ushort Port
        {
            get => transport ? transport.port : (ushort)20974;
            set
            {
                if (transport)
                {
                    transport.port = value;
                }
                else
                {
                    Debug.LogWarning("The NetworkManager has no Transport component");
                }
            }
        }

        /// <summary>
        /// 初始化配置传输
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            SetMode(NetworkMode.None);
        }

        /// <summary>
        /// 设置游戏模式
        /// </summary>
        /// <param name="networkMode">网络模式</param>
        private void SetMode(NetworkMode networkMode)
        {
            this.networkMode = networkMode;
            if (transport == null)
            {
                Debug.LogError("The NetworkManager has no Transport component.");
                return;
            }

            Transport.current = transport;
            Application.runInBackground = true;
        }

        /// <summary>
        /// 开启服务器
        /// </summary>
        /// <param name="isListen">设置false则为单机模式，不进行网络传输</param>
        public void StartServer(bool isListen = true)
        {
            if (ServerManager.isActive)
            {
                Debug.LogWarning("Server already started.");
                return;
            }

#if UNITY_SERVER
            Application.targetFrameRate = tickRate;
#endif
            SetMode(NetworkMode.Server);
            ServerManager.StartServer(isListen);
            RegisterServerEvent();
            OnStartServer?.Invoke();
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void StopServer()
        {
            if (!ServerManager.isActive) return;
            OnStopServer?.Invoke();
            ServerManager.StopServer();
            networkMode = NetworkMode.None;
            sceneName = "";
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="uri">不传入Uri则按照默认的address来匹配</param>
        public void StartClient(Uri uri = null)
        {
            if (ClientManager.isActive)
            {
                Debug.LogWarning("Client already started.");
                return;
            }

            SetMode(NetworkMode.Client);
            if (uri == null)
            {
                ClientManager.StartClient(Address, Port);
            }
            else
            {
                ClientManager.StartClient(uri);
            }

            RegisterClientEvent();
            OnStartClient?.Invoke();
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        public void StopClient()
        {
            if (networkMode == NetworkMode.None)
            {
                return;
            }

            if (networkMode == NetworkMode.Host)
            {
                OnServerDisconnectEvent(ServerManager.connection);
            }

            ClientManager.Disconnect();
            OnClientDisconnectEvent();
        }

        /// <summary>
        /// 开启主机
        /// </summary>
        /// <param name="isListen">设置false则为单机模式，不进行网络传输</param>
        public void StartHost(bool isListen = true)
        {
            if (ServerManager.isActive || ClientManager.isActive)
            {
                Debug.LogWarning("Server or Client already started.");
                return;
            }

            Debug.Log("NetworkManager --> StartHost");
            SetMode(NetworkMode.Host);
            ServerManager.StartServer(isListen);
            RegisterServerEvent();
            ClientManager.StartClient();
            RegisterClientEvent();
            ServerManager.OnClientConnect(ServerManager.connection);
            ClientManager.OnConnected?.Invoke();
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
        /// 应用退出Client和Server
        /// </summary>
        private void OnApplicationQuit()
        {
            if (ClientManager.isConnect)
            {
                StopClient();
            }

            if (ServerManager.isActive)
            {
                StopServer();
            }

            RuntimeInitializeOnLoad();
        }
    }
}