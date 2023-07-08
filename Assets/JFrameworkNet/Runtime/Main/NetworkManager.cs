using System;
using JFramework.Udp;
using UnityEngine;

// ReSharper disable All

namespace JFramework.Net
{
    public sealed partial class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;
        private string sceneName;
        private NetworkMode networkMode;
        [SerializeField] private Transport transport;
        [SerializeField] private bool runInBackground = true;
        public uint tickRate = 30;
        public uint maxConnection = 100;

        public Address address
        {
            get => transport.address;
            set => transport.address = value;
        }

        private void Awake()
        {
            Instance = this;
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
            Application.runInBackground = runInBackground;
        }

        /// <summary>
        /// 开启服务器
        /// </summary>
        /// <param name="isListen">设置false则为单机模式，不进行网络传输</param>
        public void StartServer(bool isListen = true)
        {
            if (NetworkServer.isActive)
            {
                Debug.LogWarning("Server already started.");
                return;
            }

#if UNITY_SERVER
            Application.targetFrameRate = tickRate;
#endif
            SetMode(NetworkMode.Server);
            NetworkServer.StartServer(isListen);
            RegisterServerEvent();
            OnStartServer?.Invoke();
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void StopServer()
        {
            if (!NetworkServer.isActive) return;
            OnStopServer?.Invoke();
            NetworkServer.StopServer();
            networkMode = NetworkMode.None;
            sceneName = "";
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="uri">不传入Uri则按照默认的address来匹配</param>
        public void StartClient(Uri uri = null)
        {
            if (NetworkClient.isActive)
            {
                Debug.LogWarning("Client already started.");
                return;
            }

            SetMode(NetworkMode.Client);
            if (uri == null)
            {
                NetworkClient.StartClient(address);
            }
            else
            {
                NetworkClient.StartClient(uri);
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
                OnServerDisconnectInternal(NetworkServer.client);
            }

            NetworkClient.Disconnect();
            OnClientDisconnectInternal();
        }

        /// <summary>
        /// 开启主机
        /// </summary>
        /// <param name="isListen">设置false则为单机模式，不进行网络传输</param>
        public void StartHost(bool isListen = true)
        {
            if (NetworkServer.isActive || NetworkClient.isActive)
            {
                Debug.LogWarning("Server or Client already started.");
                return;
            }

            SetMode(NetworkMode.Host);
            NetworkServer.StartServer(isListen);
            RegisterServerEvent();
            NetworkClient.StartClient();
            RegisterClientEvent();
            NetworkServer.OnClientConnect(NetworkServer.client);
            NetworkClient.connection.connecting = true;
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

        private void OnApplicationQuit()
        {
            if (NetworkClient.isConnect)
            {
                StopClient();
            }

            if (NetworkServer.isActive)
            {
                StopServer();
            }

            RuntimeInitializeOnLoad();
        }
    }
}