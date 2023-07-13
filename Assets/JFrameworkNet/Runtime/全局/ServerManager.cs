using System.Collections.Generic;
using System.Linq;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class ServerManager
    {
        /// <summary>
        /// 网络消息委托字典
        /// </summary>
        internal static readonly Dictionary<ushort, EventDelegate> events = new Dictionary<ushort, EventDelegate>();
        
        /// <summary>
        /// 服务器生成的游戏对象字典
        /// </summary>
        internal static readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();
        
        /// <summary>
        /// 连接的的客户端字典
        /// </summary>
        internal static readonly Dictionary<int, ClientEntity> clients = new Dictionary<int, ClientEntity>();
        
        /// <summary>
        /// 用来拷贝当前连接的所有客户端
        /// </summary>
        private static readonly List<ClientEntity> copies = new List<ClientEntity>();

        /// <summary>
        /// 上一次发送消息的时间
        /// </summary>
        private static double lastSendTime;

        /// <summary>
        /// 当前网络对象索引
        /// </summary>
        private static uint netId;

        /// <summary>
        /// 是否是启动的
        /// </summary>
        public static bool isActive;

        /// <summary>
        /// 是否在加载场景
        /// </summary>
        public static bool isLoadScene;
        
        /// <summary>
        /// 连接到的主机客户端
        /// </summary>
        public static ClientEntity connection;

        /// <summary>
        /// 心跳包
        /// </summary>
        private static int tickRate => NetworkManager.Instance.tickRate;
        
        /// <summary>
        /// 最大连接数量
        /// </summary>
        private static uint maxConnection => NetworkManager.Instance.maxConnection;
        
        /// <summary>
        /// 信息传送率
        /// </summary>
        private static float sendRate => tickRate < int.MaxValue ? 1f / tickRate : 0;

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
                NetworkTime.Resets();
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
            Debug.Log($"客户端 {client.clientId} 连接到服务器。");
            NetworkManager.Instance.OnServerConnectEvent(client);
        }

        /// <summary>
        /// 设置客户端准备好(可以进行消息接收)
        /// </summary>
        /// <param name="client"></param>
        internal static void SetClientReady(ClientEntity client)
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
        private static void SetClientNotReady(ClientEntity client)
        {
            Debug.Log($"设置客户端 {client.clientId} 未准备就绪");
            client.isReady = false;
            client.Send(new NotReadyEvent());
        }

        /// <summary>
        /// 设置所有客户端取消准备
        /// </summary>
        public static void SetClientNotReadyAll()
        {
            foreach (var client in clients.Values)
            {
                SetClientNotReady(client);
            }
        }
        
        /// <summary>
        /// 向所有客户端发送消息
        /// </summary>
        /// <param name="message">网络消息</param>
        /// <param name="channel">传输通道</param>
        /// <typeparam name="T"></typeparam>
        public static void SendToAll<T>(T message, Channel channel = Channel.Reliable) where T : struct, IEvent
        {
            if (!isActive)
            {
                Debug.LogWarning("服务器不是活跃的。");
                return;
            }

            using var writer = NetworkWriter.Pop();
            NetworkEvent.WriteEvent(writer, message);
            var segment = writer.ToArraySegment();
            foreach (var client in clients.Values)
            {
                client.Send(segment, channel);
            }
        }
        
        
        /// <summary>
        /// 停止服务器
        /// </summary>
        public static void StopServer()
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
            netId = 0;
            spawns.Clear();
            events.Clear();
            clients.Clear();
            connection = null;
            isLoadScene = false;
        }
    }
}