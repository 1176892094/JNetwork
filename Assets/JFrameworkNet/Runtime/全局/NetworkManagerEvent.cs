using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
#if UNITY_EDITOR
        [FoldoutGroup("服务器设置"), ShowInInspector]
        private ClientEntity serverConnection => ServerManager.connection;

        [FoldoutGroup("服务器设置"), ShowInInspector]
        private Dictionary<ushort, EventDelegate> serverEvent => ServerManager.events;

        [FoldoutGroup("服务器设置"), ShowInInspector]
        private Dictionary<uint, NetworkObject> serverSpawns => ServerManager.spawns;

        [FoldoutGroup("服务器设置"), ShowInInspector]
        private Dictionary<int, ClientEntity> connections => ServerManager.clients;

        [FoldoutGroup("客户端设置"), ShowInInspector]
        private ServerEntity clientConnection => ClientManager.connection;

        [FoldoutGroup("客户端设置"), ShowInInspector]
        private NetworkReaders readers => ClientManager.readers;

        [FoldoutGroup("客户端设置"), ShowInInspector]
        private Dictionary<ushort, EventDelegate> clientEvent => ClientManager.events;

        [FoldoutGroup("客户端设置"), ShowInInspector]
        private Dictionary<uint, NetworkObject> clientSpawns => ClientManager.spawns;

        [FoldoutGroup("客户端设置"), ShowInInspector]
        private Dictionary<uint, GameObject> assetPrefabs => ClientManager.prefabs;

        [FoldoutGroup("客户端设置"), ShowInInspector]
        private Dictionary<ulong, NetworkObject> scenePrefabs => ClientManager.scenes;

        [FoldoutGroup("客户端设置"), ShowInInspector]
        private bool isAuthority => ClientManager.isAuthority;
#endif

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