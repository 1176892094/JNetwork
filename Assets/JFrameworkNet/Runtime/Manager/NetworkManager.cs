using System;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;
        public static NetworkClient Client;
        public static NetworkServer Server;
        public static NetworkSceneManager Scene;
        public Address address => transport.address;
        [SerializeField] private Transport transport;
        [SerializeField] private bool runInBackground = true;
        [SerializeField] private int tickRate = 30;
        [SerializeField] private int maxConnection = 100;
        private NetworkMode networkMode;

        private void Awake()
        {
            if (!SetSingleton()) return;
            Client = new NetworkClient();
            Scene = new NetworkSceneManager();
        }

        /// <summary>
        /// 设置单例
        /// </summary>
        /// <returns>返回是否设置成功</returns>
        private bool SetSingleton()
        {
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
        /// 启动服务器
        /// </summary>
        /// <param name="isListen">单机模式设置为false</param>
        public void StartServer(bool isListen = true)
        {
            if (Server.isActive)
            {
                Debug.LogWarning("Server already started.");
                return;
            }

            networkMode = NetworkMode.Server;
            SetServer(isListen);
            Server.SpawnObjects();
            RegisterServerEvent();
        }

        private void SetServer(bool isListen)
        {
            SetSingleton();
#if UNITY_SERVER
            Application.targetFrameRate = tickRate;
#endif
            Server = new NetworkServer(maxConnection, isListen);
        }

        private void RegisterServerEvent()
        {
            Server.OnConnected = OnServerConnectInternal;
        }

        private static void OnServerConnectInternal(ClientConnection client)
        {
            client.isAuthority = true;
            if (Scene.localScene != "")
            {
                var message = new SceneMessage()
                {
                    sceneName = Scene.localScene
                };
                client.Send(message);
            }

            OnServerConnect?.Invoke(client);
        }

        public static event Action OnStartHost;
        public static event Action OnStopHost;
        public static event Action OnStartClient;
        public static event Action OnStopClient;
        public static event Action OnStartServer;
        public static event Action OnStopServer;
        public static event Action OnClientConnect;
        public static event Action OnClientDisconnect;
        public static event Action OnClientNotReady;
        public static event Action<ClientConnection> OnServerConnect;
        public static event Action<ClientConnection> OnServerDisconnect;
        public static event Action<ClientConnection> OnServerReady;

        /// <summary>
        /// 运行初始化
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitializeOnLoad()
        {
            Instance = null;
            Client = null;
            Server = null;
            Scene = null;
            OnStartHost = null;
            OnStopHost = null;
            OnStartClient = null;
            OnStopClient = null;
            OnStartServer = null;
            OnStopServer = null;
            OnClientConnect = null;
            OnClientDisconnect = null;
            OnClientNotReady = null;
            OnServerConnect = null;
            OnServerDisconnect = null;
            OnServerReady = null;
        }
    }
}