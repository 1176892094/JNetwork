using System;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
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
        // public static event Action<string> OnClientLoadScene;
        // public static event Action<string> OnServerLoadScene;
        // public static event Action<string> OnClientSceneChanged;
        // public static event Action<string> OnServerSceneChanged;

        /// <summary>
        /// 运行初始化
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitializeOnLoad()
        {
            Transport.RuntimeInitializeOnLoad();
            NetworkEvent.RuntimeInitializeOnLoad();
            NetworkTime.RuntimeInitializeOnLoad();
            NetworkClient.RuntimeInitializeOnLoad();
            NetworkServer.StopServer();
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
            // OnClientLoadScene = null;
            // OnServerLoadScene = null;
            // OnClientSceneChanged = null;
            // OnServerSceneChanged = null;
        }
    }
}