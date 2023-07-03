using System;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;
        public Address address => transport.address;
        [SerializeField] private Transport transport;
        [SerializeField] private bool runInBackground = true;
        [SerializeField] private bool dontListen;
        public int hearTickRate = 30;
        public int maxConnection = 100;
        private string sceneName;
        private NetworkMode networkMode;

        private void Awake()
        {
            SetSingleton(NetworkMode.None);
        }

        /// <summary>
        /// 设置单例
        /// </summary>
        /// <returns>返回是否设置成功</returns>
        private bool SetSingleton(NetworkMode networkMode)
        {
            this.networkMode = networkMode;
            if (Instance != null && Instance == this)
            {
                return true;
            }

            if (transport == null)
            {
                if (TryGetComponent(out Transport newTransport))
                {
                    transport = newTransport;
                }
                else
                {
                    Debug.LogError("The NetworkManager has no Transport component.");
                    return false;
                }
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Transport.Instance = transport;
            Application.runInBackground = runInBackground;
            return true;
        }

        /// <summary>
        /// 启动Server
        /// </summary>
        public void StartServer()
        {
            if (NetworkServer.isActive)
            {
                Debug.LogWarning("Server already started.");
                return;
            }

            SetSingleton(NetworkMode.Server);
#if UNITY_SERVER
            Application.targetFrameRate = heartRate;
#endif
            NetworkServer.Connect();
            RegisterServerEvent();
            NetworkServer.SpawnObjects();
        }
        
        public void StartClient()
        {
            if (NetworkClient.isActive)
            {
                Debug.LogWarning("Client already started.");
                return;
            }
            
            SetSingleton(NetworkMode.Client);
            RegisterClientEvent();
            NetworkClient.Connect(address);
            OnStartClient?.Invoke();
        }

        public void StartClient(Uri uri)
        {
            if (NetworkClient.isActive)
            {
                Debug.LogWarning("Client already started.");
                return;
            }
            
            SetSingleton(NetworkMode.Client);
            RegisterClientEvent();
            NetworkClient.Connect(uri);
            OnStartClient?.Invoke();
        }

        private void RegisterServerEvent()
        {
            NetworkServer.OnConnected = OnServerConnectInternal;
            NetworkServer.OnDisconnected = OnServerDisconnectInternal;
            // NetworkServer.RegisterHandler<ReadyMessage>(OnServerReadyInternal);
        }
        
        private void RegisterClientEvent()
        {
            NetworkClient.OnConnected = OnClientConnectInternal;
            NetworkClient.OnDisconnected = OnClientDisconnectInternal;
            // NetworkServer.RegisterHandler<ReadyMessage>(OnServerReadyInternal);
        }

        private void OnServerConnectInternal(ClientConnection client)
        {
            client.isAuthority = true;
            if (sceneName != "")
            {
                var message = new SceneMessage()
                {
                    sceneName = sceneName
                };
                client.Send(message);
            }

            OnServerConnect?.Invoke(client);
        }

        private void OnServerDisconnectInternal(ClientConnection client)
        {
            OnServerDisconnect?.Invoke(client);
        }
        
        private void OnClientConnectInternal()
        {
            NetworkClient.connection.isAuthority = true;
            if (!NetworkClient.isReady)
            {
                NetworkClient.Ready();
            }

            OnClientConnect?.Invoke();
        }

        private void OnClientDisconnectInternal()
        {
            if (networkMode is NetworkMode.Server or NetworkMode.None) return;
            networkMode = networkMode == NetworkMode.Host ? NetworkMode.Server : NetworkMode.None;
            OnClientDisconnect?.Invoke();
            OnStopClient?.Invoke();
            NetworkClient.Reset();
            if (networkMode == NetworkMode.Server) return;
            sceneName = "";
        }
    }
}