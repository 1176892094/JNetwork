using System;
using System.Collections.Generic;
using System.Linq;
using JFramework.Interface;
using JFramework.Udp;
using UnityEngine;

// ReSharper disable All

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        /// <summary>
        /// 网络消息委托字典
        /// </summary>
        private static readonly Dictionary<ushort, EventDelegate> messages = new Dictionary<ushort, EventDelegate>();
        
        /// <summary>
        /// 服务器生成的游戏对象字典
        /// </summary>
        internal static readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();
        
        /// <summary>
        /// 连接的的客户端字典
        /// </summary>
        private static readonly Dictionary<int, ClientEntity> clients = new Dictionary<int, ClientEntity>();
        
        /// <summary>
        /// 用来拷贝当前连接的所有客户端
        /// </summary>
        private static readonly List<ClientEntity> copies = new List<ClientEntity>();

        /// <summary>
        /// 上一次发送消息的时间
        /// </summary>
        private static double lastSendTime;

        /// <summary>
        /// 是否初始化
        /// </summary>
        private static bool isInit;

        /// <summary>
        /// 是否是启动的
        /// </summary>
        public static bool isActive => isInit;
        
        /// <summary>
        /// 是否在加载场景
        /// </summary>
        public static bool isLoadScene;
        
        /// <summary>
        /// 连接到的主机客户端
        /// </summary>
        public static ClientEntity connection;
        
        /// <summary>
        /// 当有客户端连接到服务器的事件
        /// </summary>
        internal static Action<ClientEntity> OnConnected;
        
        /// <summary>
        /// 当有客户端从服务器断开的事件
        /// </summary>
        internal static Action<ClientEntity> OnDisconnected;
        
        /// <summary>
        /// 心跳包
        /// </summary>
        private static uint tickRate => NetworkManager.Instance.tickRate;
        
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

            if (!isInit)
            {
                isInit = true;
                clients.Clear();
                RegisterEvent();
                RegisterTransport();
                NetworkTime.RuntimeInitializeOnLoad();
                Debug.Log("NetworkServer --> StartServer");
            }

            SpawnObjects();
        }

        /// <summary>
        /// 当客户端连接
        /// </summary>
        /// <param name="client">连接的客户端实体</param>
        internal static void OnClientConnect(ClientEntity client)
        {
            if (!clients.ContainsKey(client.clientId))
            {
                clients[client.clientId] = client;
            }

            Debug.Log($"NetworkServer --> Connected: {client.clientId}");
            OnConnected?.Invoke(client);
        }

        /// <summary>
        /// 设置客户端准备好(可以进行消息接收)
        /// </summary>
        /// <param name="client"></param>
        internal static void SetClientReady(ClientEntity client)
        {
            client.isReady = true;
            SpawnObjectForClient(client);
        }

        /// <summary>
        /// 设置客户端未准备(不能进行消息接收)
        /// </summary>
        /// <param name="client"></param>
        private static void SetClientNotReady(ClientEntity client)
        {
            client.isReady = false;
            client.Send(new NotReadyEvent());
        }

        /// <summary>
        /// 设置所有客户端取消准备
        /// </summary>
        public static void SetClientNotReadyAll()
        {
            foreach (var connection in clients.Values)
            {
                SetClientNotReady(connection);
            }
        }

        /// <summary>
        /// 服务器给指定客户端生成游戏对象
        /// </summary>
        /// <param name="client">传入指定客户端</param>
        private static void SpawnObjectForClient(ClientEntity client)
        {
            if (!client.isReady) return;
            client.Send(new ObjectSpawnStartEvent());

            foreach (var @object in spawns.Values)
            {
                if (@object.gameObject.activeSelf)
                {
                    @object.AddObserver(client);
                }
            }
        }
        
        /// <summary>
        /// 服务器向指定客户端发送生成对象的消息
        /// </summary>
        /// <param name="client">指定的客户端</param>
        /// <param name="object">生成的游戏对象</param>
        internal static void SendSpawnMessage(ClientEntity client, NetworkObject @object)
        {
            using (NetworkWriter owner = NetworkWriter.Pop(), observers = NetworkWriter.Pop())
            {
                bool isOwner = @object.connection == client;
                ArraySegment<byte> segment = CreateSpawnMessagePayload(@object, isOwner, owner, observers);
                SpawnEvent message = new SpawnEvent
                {
                    netId = @object.netId,
                    sceneId = @object.sceneId,
                    assetId = @object.assetId,
                    isOwner = @object.connection == client,
                    position = @object.transform.localPosition,
                    rotation = @object.transform.localRotation,
                    localScale = @object.transform.localScale,
                    segment = segment
                };
                client.Send(message);
            }
        }

        /// <summary>
        /// 序列化网络对象，并将数据转发给客户端
        /// </summary>
        /// <param name="object">网络对象生成</param>
        /// <param name="isOwner">是否包含权限</param>
        /// <param name="owner">有权限的</param>
        /// <param name="observers"></param>
        /// <returns></returns>
        private static ArraySegment<byte> CreateSpawnMessagePayload(NetworkObject @object, bool isOwner, NetworkWriter owner, NetworkWriter observers)
        {
            if (@object.objects.Length == 0) return default;
            @object.SerializeServer(true, owner, observers);
            ArraySegment<byte> segment = isOwner ? owner.ToArraySegment() : observers.ToArraySegment();
            return segment;
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
                Debug.LogWarning("NetworkServer is not active");
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
        /// 向所有准备好的客户端发送消息
        /// </summary>
        /// <param name="message">网络消息</param>
        /// <param name="channel">传输通道</param>
        /// <typeparam name="T"></typeparam>
        public static void SendToReady<T>(T message, Channel channel = Channel.Reliable) where T : struct, IEvent
        {
            if (!isActive)
            {
                Debug.LogWarning("NetworkServer is not active");
                return;
            }

            using var writer = NetworkWriter.Pop();
            NetworkEvent.WriteEvent(writer, message);
            var segment = writer.ToArraySegment();
            foreach (var client in clients.Values.Where(client => client.isReady))
            {
                client.Send(segment, channel);
            }
        }

        /// <summary>
        /// 生成物体
        /// </summary>
        internal static void SpawnObjects()
        {
        }

        /// <summary>
        /// 断开所有客户端连接
        /// </summary>
        private static void DisconnectToAll()
        {
            foreach (var client in clients.Values.ToList())
            {
                client.Disconnect();
                if (client.clientId != NetworkConst.HostId)
                {
                    OnServerDisconnected(client.clientId);
                }
            }
        }
        
        /// <summary>
        /// 停止服务器
        /// </summary>
        public static void StopServer()
        {
            if (isInit)
            {
                isInit = false;
                DisconnectToAll();
                Transport.current.StopServer();
                UnRegisterTransport();
            }

            connection = null;
            spawns.Clear();
            clients.Clear();
            messages.Clear();
            isLoadScene = false;
            OnConnected = null;
            OnDisconnected = null;
        }
    }
}