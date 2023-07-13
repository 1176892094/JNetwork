using System;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        /// <summary>
        /// 开启主机的事件
        /// </summary>
        public static event Action OnStartHost;
        
        /// <summary>
        /// 停止主机的事件
        /// </summary>
        public static event Action OnStopHost;
        
        /// <summary>
        /// 开启客户端的事件
        /// </summary>
        public static event Action OnStartClient;
        
        /// <summary>
        /// 停止客户端的事件(包含主机)
        /// </summary>
        public static event Action OnStopClient;
        
        /// <summary>
        /// 开启服务器的事件
        /// </summary>
        public static event Action OnStartServer;
        
        /// <summary>
        /// 停止服务器的事件(包含主机)
        /// </summary>
        public static event Action OnStopServer;
        
        /// <summary>
        /// 客户端连接的事件(包含主机)
        /// </summary>
        public static event Action OnClientConnect;
        
        /// <summary>
        /// 客户端断开的事件
        /// </summary>
        public static event Action OnClientDisconnect;
        
        /// <summary>
        /// 客户端取消准备的事件
        /// </summary>
        public static event Action OnClientNotReady;
        
        /// <summary>
        /// 有客户端连接到服务器的事件
        /// </summary>
        public static event Action<ClientEntity> OnServerConnect;
        
        /// <summary>
        /// 有客户端从服务器断开的事件
        /// </summary>
        public static event Action<ClientEntity> OnServerDisconnect;
        
        /// <summary>
        /// 客户端在服务器准备就绪的事件
        /// </summary>
        public static event Action<ClientEntity> OnServerReady;
        
        /// <summary>
        /// 客户端加载场景的事件
        /// </summary>
        public static event Action<string> OnClientLoadScene;
        
        /// <summary>
        /// 服务器加载场景的事件
        /// </summary>
        public static event Action<string> OnServerLoadScene;
        
        /// <summary>
        /// 客户端加载场景完成的事件
        /// </summary>
        public static event Action<string> OnClientSceneChanged;
        
        /// <summary>
        /// 服务器加载场景完成的事件
        /// </summary>
        public static event Action<string> OnServerSceneChanged;

        /// <summary>
        /// 运行初始化
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitializeOnLoad()
        {
            Transport.Resets();
            NetworkTime.Resets();
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
            OnClientLoadScene = null;
            OnServerLoadScene = null;
            OnClientSceneChanged = null;
            OnServerSceneChanged = null;
        }
    }
}