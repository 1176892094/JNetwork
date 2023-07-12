using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager : GlobalSingleton<NetworkManager>
    {
        /// <summary>
        /// 服务器场景名称
        /// </summary>
        private string sceneName;

        /// <summary>
        /// 网络传输组件
        /// </summary>
        [SerializeField] private Transport transport;
        
        /// <summary>
        /// 网络预置体设置
        /// </summary>
        [SerializeField] private NetworkSetting setting;
        
        /// <summary>
        /// 心跳传输率
        /// </summary>
        public uint tickRate = 30;
        
        /// <summary>
        /// 客户端最大连接数量
        /// </summary>
        public uint maxConnection = 100;

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
        private NetworkMode networkMode
        {
            get
            {
                if (ServerManager.isActive)
                {
                    return ClientManager.isActive ? NetworkMode.Host : NetworkMode.Server;
                }

                return ClientManager.isActive ? NetworkMode.Client : NetworkMode.None;
            }
        }

        /// <summary>
        /// 初始化配置传输
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (transport == null)
            {
                Debug.LogError("NetworkManager 没有 Transport 组件。");
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
                Debug.LogWarning("服务器已经连接！");
                return;
            }

#if UNITY_SERVER
            Application.targetFrameRate = tickRate;
#endif
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
                Debug.LogWarning("客户端已经连接！");
                return;
            }
            
            if (uri == null)
            {
                ClientManager.StartClient(address, port);
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
                Debug.LogWarning("客户端或服务器已经连接！");
                return;
            }

            Debug.Log("开启主机。");
            ServerManager.StartServer(isListen);
            RegisterServerEvent();
            ClientManager.StartClient();
            RegisterClientEvent();
            ServerManager.OnClientConnect(ServerManager.connection);
            OnClientConnectEvent();
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