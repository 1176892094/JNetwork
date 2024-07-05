// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-04  23:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    public partial class ServerManager : Component<NetworkManager>
    {
        /// <summary>
        /// 网络消息委托字典
        /// </summary>
        [ShowInInspector] internal readonly Dictionary<ushort, MessageDelegate> messages = new Dictionary<ushort, MessageDelegate>();

        /// <summary>
        /// 连接的的客户端字典
        /// </summary>
        [ShowInInspector] internal readonly Dictionary<int, NetworkClient> clients = new Dictionary<int, NetworkClient>();

        /// <summary>
        /// 服务器生成的游戏对象字典
        /// </summary>
        [ShowInInspector] internal readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();

        /// <summary>
        /// 用来拷贝当前连接的所有客户端
        /// </summary>
        private List<NetworkClient> copies = new List<NetworkClient>();

        /// <summary>
        /// 上一次发送消息的时间
        /// </summary>
        [SerializeField] private double sendTime;

        /// <summary>
        /// 是否是启动的
        /// </summary>
        [ShowInInspector]
        public bool isActive { get; private set; }

        /// <summary>
        /// 所有客户端都准备
        /// </summary>
        [ShowInInspector]
        public bool isReady => clients.Values.All(client => client.isReady);

        /// <summary>
        /// 是否在加载场景
        /// </summary>
        public bool isLoadScene { get; internal set; }

        /// <summary>
        /// 连接客户端数量
        /// </summary>
        public int connections => clients.Count;

        /// <summary>
        /// 当前网络对象Id
        /// </summary>
        private uint objectId;

        /// <summary>
        /// 有客户端连接到服务器的事件
        /// </summary>
        public event Action<NetworkClient> OnConnect;

        /// <summary>
        /// 有客户端从服务器断开的事件
        /// </summary>
        public event Action<NetworkClient> OnDisconnect;

        /// <summary>
        /// 客户端在服务器准备就绪的事件
        /// </summary>
        public event Action<NetworkClient> OnReady;

        /// <summary>
        /// 开启服务器
        /// </summary>
        /// <param name="mode"></param>
        internal void StartServer(EntryMode mode)
        {
            if (mode is EntryMode.Server or EntryMode.Host)
            {
                NetworkManager.Transport.StartServer();
            }

            Register();
            isActive = true;
            clients.Clear();
            NetworkManager.Time.Reset();
            SpawnObjects();
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        internal void StopServer()
        {
            if (!isActive) return;
            isActive = false;
            Debug.Log("停止服务器。");
            copies = clients.Values.ToList();
            foreach (var client in copies)
            {
                client.Disconnect();
                if (client.clientId != Const.HostId)
                {
                    OnServerDisconnect(client.clientId);
                }
            }

            if (NetworkManager.Transport != null)
            {
                NetworkManager.Transport.StopServer();
            }

            sendTime = 0;
            objectId = 0;
            spawns.Clear();
            clients.Clear();
            messages.Clear();
            isLoadScene = false;
        }

        /// <summary>
        /// 当客户端连接到服务器
        /// </summary>
        /// <param name="client">连接的客户端实体</param>
        internal void Connect(NetworkClient client)
        {
            if (clients.TryAdd(client.clientId, client))
            {
                client.isPlayer = true;
                OnConnect?.Invoke(client);
            }
        }
    }

    public partial class ServerManager
    {
        /// <summary>
        /// 注册服务器消息消息
        /// </summary>
        private void Register()
        {
            NetworkManager.Transport.OnServerConnect -= OnServerConnect;
            NetworkManager.Transport.OnServerDisconnect -= OnServerDisconnect;
            NetworkManager.Transport.OnServerReceive -= OnServerReceive;
            NetworkManager.Transport.OnServerConnect += OnServerConnect;
            NetworkManager.Transport.OnServerDisconnect += OnServerDisconnect;
            NetworkManager.Transport.OnServerReceive += OnServerReceive;
            messages[Message<PingMessage>.Id] = NetworkUtility.GetMessage<PingMessage>(PingMessage);
            messages[Message<ReadyMessage>.Id] = NetworkUtility.GetMessage<ReadyMessage>(ReadyMessage);
            messages[Message<EntityMessage>.Id] = NetworkUtility.GetMessage<EntityMessage>(EntityMessage);
            messages[Message<ServerRpcMessage>.Id] = NetworkUtility.GetMessage<ServerRpcMessage>(ServerRpcMessage);
        }

        internal void PingMessage(NetworkClient client, PingMessage message)
        {
            client.Send(new PingMessage(message.clientTime), Channel.Unreliable);
        }

        internal void ReadyMessage(NetworkClient client, ReadyMessage message)
        {
            client.isReady = true;
            foreach (var @object in spawns.Values.Where(@object => @object.gameObject.activeSelf))
            {
                SpawnToClient(client, @object);
            }

            NetworkManager.Instance.SpawnPlayer(client);
            OnReady?.Invoke(client);
        }

        internal void EntityMessage(NetworkClient client, EntityMessage message)
        {
            if (!spawns.TryGetValue(message.objectId, out var @object))
            {
                Debug.LogWarning($"无法为客户端 {client.clientId} 同步网络对象 {message.objectId}。");
                return;
            }

            if (@object == null)
            {
                Debug.LogWarning($"无法为客户端 {client.clientId} 同步网络对象 {message.objectId}。");
                return;
            }

            if (@object.connection != client)
            {
                Debug.LogWarning($"无法为客户端 {client.clientId} 同步网络对象 {message.objectId}。");
                return;
            }

            using var reader = NetworkReader.Pop(message.segment);
            if (!@object.ServerDeserialize(reader))
            {
                Debug.LogWarning($"无法反序列化对象：{@object.name}。对象Id：{@object.objectId}");
                client.Disconnect();
            }
        }

        /// <summary>
        /// 客户端发送ServerRpc请求，服务器接受后调用
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        internal void ServerRpcMessage(NetworkClient client, ServerRpcMessage message, int channel)
        {
            if (!client.isReady)
            {
                if (channel == Channel.Reliable)
                {
                    Debug.LogWarning("客户端需要 Ready 后才能接收网络消息");
                }

                return;
            }

            if (!spawns.TryGetValue(message.objectId, out var @object))
            {
                Debug.LogWarning($"没有找到发送 ServerRpc 的对象。对象网络Id：{message.objectId}");
                return;
            }

            if (NetworkDelegate.Contains(message.methodHash) && @object.connection != client)
            {
                Debug.LogWarning($"接收到 ServerRpc 但对象没有通过验证。对象网络Id：{message.objectId}");
                return;
            }

            using var reader = NetworkReader.Pop(message.segment);
            @object.InvokeMessage(message.componentId, message.methodHash, InvokeMode.ServerRpc, reader, client);
        }
    }

    public partial class ServerManager
    {
        /// <summary>
        /// 指定客户端连接到服务器
        /// </summary>
        /// <param name="clientId"></param>
        private void OnServerConnect(int clientId)
        {
            if (clientId == 0)
            {
                Debug.LogError($"无效的客户端连接。客户端：{clientId}");
                NetworkManager.Transport.StopClient(clientId);
            }
            else if (clients.ContainsKey(clientId))
            {
                NetworkManager.Transport.StopClient(clientId);
            }
            else if (clients.Count >= NetworkManager.Instance.connection)
            {
                NetworkManager.Transport.StopClient(clientId);
            }
            else
            {
                Connect(new NetworkClient(clientId));
            }
        }

        /// <summary>
        /// 服务器断开指定客户端连接
        /// </summary>
        /// <param name="clientId"></param>
        internal void OnServerDisconnect(int clientId)
        {
            if (clients.TryGetValue(clientId, out var client))
            {
                var objects = spawns.Values.Where(@object => @object.connection == client).ToList();
                foreach (var @object in objects)
                {
                    Destroy(@object);
                }

                clients.Remove(client.clientId);
                OnDisconnect?.Invoke(client);
            }
        }

        /// <summary>
        /// 服务器从传输接收数据
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="segment"></param>
        /// <param name="channel"></param>
        internal void OnServerReceive(int clientId, ArraySegment<byte> segment, int channel)
        {
            if (!clients.TryGetValue(clientId, out var client))
            {
                Debug.LogError($"服务器接收到消息。未知的客户端：{clientId}");
                return;
            }

            if (!client.readerPool.Write(segment))
            {
                Debug.LogWarning($"无法将消息写入。断开客户端：{client}");
                client.Disconnect();
                return;
            }

            while (!isLoadScene && client.readerPool.TryRead(out var reader, out var remoteTime))
            {
                if (reader.residue < Const.MessageSize)
                {
                    Debug.LogError($"网络消息应该有个开始的Id。断开客户端：{client}");
                    client.Disconnect();
                    return;
                }

                var message = reader.ReadUShort();
                if (!messages.TryGetValue(message, out var action))
                {
                    Debug.LogError($"未知的网络消息Id：{message} 断开客户端：{client}");
                    client.Disconnect();
                    return;
                }

                client.remoteTime = remoteTime;
                action.Invoke(client, reader, channel);
            }

            if (!isLoadScene && client.readerPool.Count > 0)
            {
                Debug.LogError($"有残留消息没被写入！残留数：{client.readerPool.Count}");
            }
        }
    }

    public partial class ServerManager
    {
        /// <summary>
        /// 生成网络对象
        /// </summary>
        internal void SpawnObjects()
        {
            var objects = Resources.FindObjectsOfTypeAll<NetworkObject>();
            foreach (var @object in objects)
            {
                if (NetworkUtility.IsSceneObject(@object) && @object.objectId == 0)
                {
                    @object.gameObject.SetActive(true);
                    var parent = @object.transform.parent;
                    if (parent == null || parent.gameObject.activeInHierarchy)
                    {
                        Spawn(@object.gameObject, @object.connection);
                    }
                }
            }
        }

        /// <summary>
        /// 仅在Server和Host能使用，生成物体的方法
        /// </summary>
        /// <param name="obj">生成的游戏物体</param>
        /// <param name="client">客户端Id</param>
        public void Spawn(GameObject obj, NetworkClient client = null)
        {
            if (!isActive)
            {
                Debug.LogError($"服务器不是活跃的。", obj);
                return;
            }

            if (!obj.TryGetComponent(out NetworkObject @object))
            {
                Debug.LogError($"生成对象 {obj} 没有 NetworkObject 组件", obj);
                return;
            }

            if (spawns.ContainsKey(@object.objectId))
            {
                Debug.LogWarning($"网络对象 {@object} 已经被生成。", @object.gameObject);
                return;
            }

            @object.connection = client;

            if (NetworkManager.Mode == EntryMode.Host)
            {
                if (@object.connection?.clientId == Const.HostId)
                {
                    @object.isOwner = true;
                }
            }

            if (!@object.isServer && @object.objectId == 0)
            {
                @object.objectId = ++objectId;
                @object.isServer = true;
                @object.isClient = NetworkManager.Client.isActive;
                spawns[@object.objectId] = @object;
                @object.OnStartServer();
            }

            SpawnToClients(@object);
        }

        /// <summary>
        /// 遍历所有客户端，发送生成物体的消息
        /// </summary>
        /// <param name="object">传入对象</param>
        private void SpawnToClients(NetworkObject @object)
        {
            foreach (var client in clients.Values.Where(client => client.isReady))
            {
                SpawnToClient(client, @object);
            }
        }

        /// <summary>
        /// 服务器向指定客户端发送生成对象的消息
        /// </summary>
        /// <param name="client">指定的客户端</param>
        /// <param name="object">生成的游戏对象</param>
        private void SpawnToClient(NetworkClient client, NetworkObject @object)
        {
            using NetworkWriter writer = NetworkWriter.Pop(), observer = NetworkWriter.Pop();
            var isOwner = @object.connection == client;
            var transform = @object.transform;
            var message = new SpawnMessage
            {
                isOwner = isOwner,
                sceneId = @object.sceneId,
                assetId = @object.assetId,
                objectId = @object.objectId,
                position = transform.localPosition,
                rotation = transform.localRotation,
                localScale = transform.localScale,
                segment = SerializeObject(@object, isOwner, writer, observer)
            };
            client.Send(message);
        }

        /// <summary>
        /// 序列化网络对象，并将数据转发给客户端
        /// </summary>
        /// <param name="object">网络对象生成</param>
        /// <param name="isOwner"></param>
        /// <param name="owner"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        private ArraySegment<byte> SerializeObject(NetworkObject @object, bool isOwner, NetworkWriter owner, NetworkWriter observer)
        {
            if (@object.entities.Length == 0)
            {
                return default;
            }

            @object.ServerSerialize(true, owner, observer);
            return isOwner ? owner : observer;
        }

        /// <summary>
        /// 将网络对象销毁
        /// </summary>
        /// <param name="object"></param>
        public void Destroy(NetworkObject @object)
        {
            spawns.Remove(@object.objectId);
            @object.isDestroy = true;
            foreach (var client in clients.Values)
            {
                client.Send(new DestroyMessage(@object.objectId));
            }

            if (NetworkManager.Mode == EntryMode.Host)
            {
                @object.OnStopClient();
                @object.isOwner = false;
                @object.OnNotifyAuthority();
                NetworkManager.Client.spawns.Remove(@object.objectId);
            }

            @object.OnStopServer();
            Destroy(@object.gameObject);
        }

        /// <summary>
        /// 将网络对象重置并隐藏
        /// </summary>
        /// <param name="object"></param>
        public void Despawn(NetworkObject @object)
        {
            spawns.Remove(@object.objectId);
            foreach (var client in clients.Values)
            {
                client.Send(new DespawnMessage(@object.objectId));
            }

            if (NetworkManager.Mode == EntryMode.Host)
            {
                @object.OnStopClient();
                @object.isOwner = false;
                @object.OnNotifyAuthority();
                NetworkManager.Client.spawns.Remove(@object.objectId);
            }

            @object.OnStopServer();
            @object.gameObject.SetActive(false);
            @object.Reset();
        }
    }

    public partial class ServerManager
    {
        internal void EarlyUpdate()
        {
            if (NetworkManager.Transport != null)
            {
                NetworkManager.Transport.ServerEarlyUpdate();
            }
        }

        internal void AfterUpdate()
        {
            if (isActive)
            {
                if (TimeManager.Ticks(NetworkManager.SendRate, ref sendTime))
                {
                    Broadcast();
                }
            }

            if (NetworkManager.Transport != null)
            {
                NetworkManager.Transport.ServerAfterUpdate();
            }
        }

        /// <summary>
        /// 服务器对所有客户端进行广播和更新
        /// </summary>
        private void Broadcast()
        {
            copies.Clear();
            copies.AddRange(clients.Values);
            foreach (var client in copies)
            {
                if (client.isReady)
                {
                    foreach (var @object in spawns.Values)
                    {
                        if (@object == null)
                        {
                            Debug.LogWarning($"在客户端 {client.clientId} 找到了空的网络对象。");
                            return;
                        }

                        var serialize = @object.ServerSerialize(Time.frameCount);
                        if (@object.connection == client)
                        {
                            if (serialize.owner.position > 0)
                            {
                                client.Send(new EntityMessage(@object.objectId, serialize.owner));
                            }
                        }
                        else
                        {
                            if (serialize.observer.position > 0)
                            {
                                client.Send(new EntityMessage(@object.objectId, serialize.observer));
                            }
                        }
                    }
                }

                client.Update();
            }
        }
    }
}