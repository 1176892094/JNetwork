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
            Transport.RestStatic();
            NetworkTime.ResetStatic();
            OnStartHost = null;
            OnStopHost = null;
            OnStartClient = null;
            OnStopClient = null;
            OnStartServer = null;
            OnStopServer = null;
            OnClientLoadScene = null;
            OnServerLoadScene = null;
            OnClientSceneChanged = null;
            OnServerSceneChanged = null;
        }
    }
}