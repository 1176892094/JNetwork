using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JFramework.Net
{
    public sealed partial class NetworkManager : GlobalSingleton<NetworkManager>
    {
#if UNITY_EDITOR
        [FoldoutGroup("服务器设置")][ShowInInspector] private ClientEntity serverConnection => ServerManager.connection;
        [FoldoutGroup("服务器设置")][ShowInInspector] private Dictionary<ushort, EventDelegate> serverEvent => ServerManager.events;
        [FoldoutGroup("服务器设置")][ShowInInspector] private Dictionary<uint, NetworkObject> serverSpawns => ServerManager.spawns;
        [FoldoutGroup("服务器设置")][ShowInInspector] private Dictionary<int, ClientEntity> connections => ServerManager.clients;
        [FoldoutGroup("客户端设置")][ShowInInspector] private ServerEntity clientConnection => ClientManager.connection;
        [FoldoutGroup("客户端设置")][ShowInInspector] private NetworkReaders readers => ClientManager.readers;
        [FoldoutGroup("客户端设置")][ShowInInspector] private Dictionary<ushort, EventDelegate> clientEvent => ClientManager.events;
        [FoldoutGroup("客户端设置")][ShowInInspector] private Dictionary<uint, NetworkObject> clientSpawns => ClientManager.spawns;
        [FoldoutGroup("客户端设置")][ShowInInspector] private Dictionary<uint, GameObject> assetPrefabs => ClientManager.prefabs;
        [FoldoutGroup("客户端设置")][ShowInInspector] private Dictionary<ulong, NetworkObject> scenePrefabs => ClientManager.scenes;
        [FoldoutGroup("客户端设置")][ShowInInspector] private bool isAuthority => ClientManager.isAuthority;
#endif
        
        /// <summary>
        /// 服务器场景
        /// </summary>
        private static string serverScene;
        
        /// <summary>
        /// 预置体列表
        /// </summary>
        internal static readonly List<GameObject> prefabs = new List<GameObject>();

        /// <summary>
        /// 网络传输组件
        /// </summary>
        [FoldoutGroup("网络管理器"), SerializeField] private Transport transport;

        /// <summary>
        /// 心跳传输率
        /// </summary>
        [FoldoutGroup("网络管理器"), SerializeField] internal int tickRate = 30;

        /// <summary>
        /// 客户端最大连接数量
        /// </summary>
        [FoldoutGroup("网络管理器"), SerializeField] internal uint maxConnection = 100;

        /// <summary>
        /// 传输连接地址
        /// </summary>
        [FoldoutGroup("网络管理器"), ShowInInspector]
        public string address
        {
            get => transport ? transport.address : NetworkConst.Address;
            set => transport.address = transport ? value : NetworkConst.Address;
        }

        /// <summary>
        /// 传输连接端口
        /// </summary>
        [FoldoutGroup("网络管理器"), ShowInInspector]
        public ushort port
        {
            get => transport ? transport.port : NetworkConst.Port;
            set => transport.port = transport ? value : NetworkConst.Port;
        }

        /// <summary>
        /// 网络运行模式
        /// </summary>
        [FoldoutGroup("网络管理器"), ShowInInspector]
        internal static NetworkMode mode
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
            
            Application.runInBackground = true;
#if UNITY_SERVER
            Application.targetFrameRate = tickRate;
#endif
            Transport.current = transport;
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
            
            serverScene = "";
            ServerManager.StartServer(isListen);
            OnStartServer?.Invoke();
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void StopServer()
        {
            if (!ServerManager.isActive)
            {
                Debug.LogWarning("服务器已经停止！");
                return;
            }
            
            serverScene = "";
            OnStopServer?.Invoke();
            ServerManager.StopServer();
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        public void StartClient()
        {
            if (ClientManager.isActive)
            {
                Debug.LogWarning("客户端已经连接！");
                return;
            }
            
            ClientManager.StartClient(address, port);
            OnStartClient?.Invoke();
        }

        /// <summary>
        /// 开启客户端 (根据Uri来连接)
        /// </summary>
        /// <param name="uri">传入Uri</param>
        public void StartClient(Uri uri)
        {
            if (ClientManager.isActive)
            {
                Debug.LogWarning("客户端已经连接！");
                return;
            }
            
            ClientManager.StartClient(uri);
            OnStartClient?.Invoke();
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        public void StopClient()
        {
            if (!ClientManager.isActive)
            {
                Debug.LogWarning("客户端已经停止！");
                return;
            }

            if (mode == NetworkMode.Host)
            {
                OnServerDisconnectEvent(ServerManager.connection);
                ServerManager.clients.Remove(ServerManager.connection.clientId);
                ServerManager.connection = null;
            }

            OnStopClient?.Invoke();
            ClientManager.StopClient();
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
            ClientManager.StartClient();
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
        /// 自动查找所有的NetworkObject
        /// </summary>
        private void OnValidate()
        {
#if UNITY_EDITOR
            prefabs.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<NetworkObject>() != null)
                {
                    prefabs.Add(prefab);
                }
            }
#endif
        }

        /// <summary>
        /// 当程序退出，停止服务器和客户端
        /// </summary>
        private void OnApplicationQuit()
        {
            if (ClientManager.isAuthority)
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