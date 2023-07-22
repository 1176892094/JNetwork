using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        /// <summary>
        /// 网络消息委托字典
        /// </summary>
        internal static readonly Dictionary<ushort, EventDelegate> events = new Dictionary<ushort, EventDelegate>();

        /// <summary>
        /// 连接的的客户端字典
        /// </summary>
        internal static readonly Dictionary<int, ClientEntity> clients = new Dictionary<int, ClientEntity>();
        
        /// <summary>
        /// 服务器生成的游戏对象字典
        /// </summary>
        internal static readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();

        /// <summary>
        /// 用来拷贝当前连接的所有客户端
        /// </summary>
        private static readonly List<ClientEntity> copies = new List<ClientEntity>();

        /// <summary>
        /// 上一次发送消息的时间
        /// </summary>
        private static double lastSendTime;

        /// <summary>
        /// 当前网络对象Id
        /// </summary>
        private static uint objectId;

        /// <summary>
        /// 是否是启动的
        /// </summary>
        public static bool isActive { get; private set; }

        /// <summary>
        /// 是否在加载场景
        /// </summary>
        public static bool isLoadScene;

        /// <summary>
        /// 连接到的主机客户端
        /// </summary>
        public static ClientEntity connection;

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
        /// 最大连接数量
        /// </summary>
        private static uint maxConnection => NetworkManager.Instance.maxConnection;

        /// <summary>
        /// 开启服务器
        /// </summary>
        /// <param name="isListen">是否进行传输</param>
        internal static void StartServer(bool isListen)
        {
            if (isListen)
            {
                Transport.current.StartServer();
            }

            if (!isActive)
            {
                Debug.Log("开启服务器。");
                isActive = true;
                clients.Clear();
                RegisterEvent();
                RegisterTransport();
                NetworkTime.ResetStatic();
            }

            SpawnObjects();
        }

        /// <summary>
        /// 当客户端连接
        /// </summary>
        /// <param name="client">连接的客户端实体</param>
        internal static void OnClientConnect(ClientEntity client)
        {
            clients.TryAdd(client.clientId, client);
            if (!string.IsNullOrEmpty(NetworkManager.sceneName))
            {
                client.Send(new SceneEvent(NetworkManager.sceneName));
            }

            OnServerConnect?.Invoke(client);
        }

        /// <summary>
        /// 设置客户端准备好(可以进行消息接收)
        /// </summary>
        /// <param name="client"></param>
        private static void SetReadyForClient(ClientEntity client)
        {
            Debug.Log($"设置客户端 {client.clientId} 准备就绪。");
            client.isReady = true;
            foreach (var @object in spawns.Values.Where(@object => @object.gameObject.activeSelf))
            {
                SendSpawnEvent(client, @object);
            }
        }

        /// <summary>
        /// 设置客户端未准备(不能进行消息接收)
        /// </summary>
        /// <param name="client"></param>
        internal static void NotReadyForClient(ClientEntity client)
        {
            Debug.Log($"设置客户端 {client.clientId} 取消准备");
            client.isReady = false;
            client.Send(new NotReadyEvent());
        }
        
        /// <summary>
        /// 停止服务器
        /// </summary>
        internal static void StopServer()
        {
            if (!isActive) return;
            isActive = false;
            Debug.Log("停止服务器。");
            if (Transport.current != null)
            {
                Transport.current.StopServer();
            }

            foreach (var client in clients.Values.ToList())
            {
                client.Disconnect();
                if (client.clientId != NetworkConst.HostId)
                {
                    OnServerDisconnected(client.clientId);
                }
            }

            UnRegisterTransport();
            lastSendTime = 0;
            objectId = 0;
            spawns.Clear();
            events.Clear();
            clients.Clear();
            connection = null;
            isLoadScene = false;
            OnServerConnect = null;
            OnServerDisconnect = null;
            OnServerReady = null;
        }
    }
}