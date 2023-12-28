using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
#if UNITY_EDITOR
        // [ShowInInspector]
        // private ClientEntity serverConnection => Server.connection;
        //
        // [ShowInInspector]
        // private Dictionary<ushort, EventDelegate> serverEvent => Server.events;

        [ShowInInspector] private Dictionary<uint, NetworkObject> serverSpawns => Server.spawns;

        [ShowInInspector] private Dictionary<int, UnityClient> connections => Server.clients;

        // [ShowInInspector]
        // private ServerEntity clientConnection => NetworkManager.Client.connection;
        //
        // [ShowInInspector]
        // private NetworkReaderPack readers => NetworkManager.Client.readers;
        //
        // [ShowInInspector]
        // private Dictionary<ushort, EventDelegate> clientEvent => NetworkManager.Client.events;

        [ShowInInspector] private Dictionary<uint, NetworkObject> clientSpawns => Client.spawns;
        //
        // [ShowInInspector]
        // private Dictionary<uint, GameObject> assetPrefabs => NetworkManager.Client.prefabs;

        [ShowInInspector] private Dictionary<ulong, NetworkObject> scenePrefabs => Client.scenes;
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
        /// 当接收Ping
        /// </summary>
        public static event Action<double> OnClientPingUpdate;

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
            OnClientPingUpdate = null;
        }
    }
}