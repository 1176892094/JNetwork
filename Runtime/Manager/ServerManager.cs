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
        [ShowInInspector] private double sendTime;

        /// <summary>
        /// 当前网络对象Id
        /// </summary>
        [ShowInInspector] private uint objectId;

        /// <summary>
        /// 是否是启动的
        /// </summary>
        [ShowInInspector]
        public bool isActive { get; private set; }

        /// <summary>
        /// 是否在加载场景
        /// </summary>
        [ShowInInspector]
        public bool isLoadScene { get; internal set; }

        /// <summary>
        /// 连接客户端数量
        /// </summary>
        [ShowInInspector]
        public int connections => clients.Count;

        /// <summary>
        /// 所有客户端都准备
        /// </summary>
        [ShowInInspector]
        public bool isReady => clients.Values.All(client => client.isReady);

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
        public event Action<NetworkClient> OnSetReady;

        /// <summary>
        /// 开启服务器
        /// </summary>
        /// <param name="isListen">是否进行传输</param>
        internal void StartServer(bool isListen)
        {
            if (isListen)
            {
                NetworkManager.Transport.StartServer();
            }

            if (!isActive)
            {
                isActive = true;
                clients.Clear();
                Register();
                RegisterTransport();
                NetworkManager.Time.Reset();
            }

            SpawnObjects();
        }

        /// <summary>
        /// 设置客户端准备好 为客户端生成服务器的所有对象
        /// </summary>
        /// <param name="client"></param>
        /// <param name="isReady"></param>
        internal void SetReady(NetworkClient client, bool isReady)
        {
            if (isReady)
            {
                client.isReady = true;
                foreach (var @object in spawns.Values.Where(@object => @object.gameObject.activeSelf))
                {
                    SendSpawnMessage(client, @object);
                }
            }
            else
            {
                client.isReady = false;
                client.Send(new NotReadyMessage());
            }
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        internal void StopServer()
        {
            if (!isActive) return;
            Debug.Log("停止服务器。");
            isActive = false;
            copies = clients.Values.ToList();
            foreach (var client in copies)
            {
                client.Disconnect();
                if (client.clientId != NetworkConst.HostId)
                {
                    OnServerDisconnected(client.clientId);
                }
            }

            if (NetworkManager.Transport != null)
            {
                NetworkManager.Transport.StopServer();
            }

            UnRegisterTransport();
            spawns.Clear();
            clients.Clear();
            messages.Clear();
            sendTime = 0;
            objectId = 0;
            isLoadScene = false;
        }
    }

    public partial class ServerManager
    {
        /// <summary>
        /// 注册服务器消息消息
        /// </summary>
        private void Register()
        {
            Register<EntityMessage>(OnEntityByServer);
            Register<SetReadyMessage>(OnSetReadyByServer);
            Register<ServerRpcMessage>(OnServerRpcByServer);
            Register<PingMessage>(OnPingByServer);
            Register<SnapshotMessage>(OnSnapshotByServer);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private void Register<TMessage>(Action<NetworkClient, TMessage> handle) where TMessage : struct, Message
        {
            messages[NetworkMessage<TMessage>.Id] = NetworkMessage.Register(handle);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private void Register<TMessage>(Action<NetworkClient, TMessage, Channel> handle) where TMessage : struct, Message
        {
            messages[NetworkMessage<TMessage>.Id] = NetworkMessage.Register(handle);
        }

        /// <summary>
        /// 当从Transport接收到一条ServerRpc消息
        /// </summary>
        private void OnServerRpcByServer(NetworkClient client, ServerRpcMessage message, Channel channel)
        {
            if (!client.isReady)
            {
                if (channel == Channel.Reliable)
                {
                    Debug.LogWarning("接收到 ServerRpc 但客户端没有准备就绪");
                }

                return;
            }

            if (!spawns.TryGetValue(message.objectId, out var @object))
            {
                Debug.LogWarning($"没有找到发送 ServerRpc 的对象。对象网络Id：{message.objectId}");
                return;
            }
            
            if (NetworkRpc.HasAuthority(message.methodHash) && @object.connection != client)
            {
                Debug.LogWarning($"接收到 ServerRpc 但对象没有通过验证。对象网络Id：{message.objectId}");
                return;
            }

            using var reader = NetworkReader.Pop(message.segment);
            @object.InvokeRpcMessage(message.serialId, message.methodHash, RpcType.ServerRpc, reader, client);
        }

        /// <summary>
        /// 服务器发送Pong消息给指定客户端
        /// </summary>
        internal void OnPingByServer(NetworkClient client, PingMessage message)
        {
            client.Send(new PongMessage(message.clientTime), Channel.Unreliable);
        }

        /// <summary>
        /// 当接收一条快照消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private void OnSnapshotByServer(NetworkClient client, SnapshotMessage message)
        {
            client?.OnSnapshotMessage(new SnapshotTime(client.remoteTime, NetworkManager.Time.localTime));
        }

        /// <summary>
        /// 实体状态同步消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private void OnEntityByServer(NetworkClient client, EntityMessage message)
        {
            if (spawns.TryGetValue(message.objectId, out var @object) && @object != null)
            {
                if (@object.connection == client)
                {
                    using var reader = NetworkReader.Pop(message.segment);
                    if (!@object.ServerDeserialize(reader))
                    {
                        Debug.LogWarning($"无法反序列化对象：{@object.name}。对象Id：{@object.objectId}");
                        client.Disconnect();
                    }
                }
                else
                {
                    Debug.LogWarning($"网络对象 {client} 为 {@object} 发送的 EntityMessage 没有权限");
                }
            }
        }


        /// <summary>
        /// 当客户端在服务器准备就绪，向客户端发送生成物体的消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private void OnSetReadyByServer(NetworkClient client, SetReadyMessage message)
        {
            SetReady(client, true);
            NetworkManager.Instance.SpawnPrefab(client);
            OnSetReady?.Invoke(client);
        }
    }

    public partial class ServerManager
    {
        /// <summary>
        /// 添加传输事件
        /// </summary>
        private void RegisterTransport()
        {
            NetworkManager.Transport.OnServerConnected += OnServerConnected;
            NetworkManager.Transport.OnServerDisconnected += OnServerDisconnected;
            NetworkManager.Transport.OnServerReceive += OnServerReceive;
        }

        /// <summary>
        /// 移除传输事件
        /// </summary>
        private void UnRegisterTransport()
        {
            NetworkManager.Transport.OnServerConnected -= OnServerConnected;
            NetworkManager.Transport.OnServerDisconnected -= OnServerDisconnected;
            NetworkManager.Transport.OnServerReceive -= OnServerReceive;
        }

        /// <summary>
        /// 指定客户端连接到服务器
        /// </summary>
        /// <param name="clientId"></param>
        private void OnServerConnected(int clientId)
        {
            if (clientId == 0)
            {
                Debug.LogError($"无效的客户端连接。客户端：{clientId}");
                NetworkManager.Transport.ServerDisconnect(clientId);
            }
            else if (clients.ContainsKey(clientId))
            {
                NetworkManager.Transport.ServerDisconnect(clientId);
            }
            else if (clients.Count >= NetworkManager.Instance.connection)
            {
                NetworkManager.Transport.ServerDisconnect(clientId);
            }
            else
            {
                OnClientConnect(new NetworkClient(clientId));
            }
        }

        /// <summary>
        /// 当客户端连接到服务器
        /// </summary>
        /// <param name="client">连接的客户端实体</param>
        internal void OnClientConnect(NetworkClient client)
        {
            clients.TryAdd(client.clientId, client);
            client.isSpawn = true;
            OnConnect?.Invoke(client);
        }

        /// <summary>
        /// 服务器断开指定客户端连接
        /// </summary>
        /// <param name="clientId"></param>
        internal void OnServerDisconnected(int clientId)
        {
            if (clients.TryGetValue(clientId, out var client))
            {
                OnDisconnect?.Invoke(client);
                var copyList = spawns.Values.Where(@object => @object.connection == client).ToList();
                foreach (var @object in copyList)
                {
                    Destroy(@object);
                }

                clients.Remove(client.clientId);
            }
        }

        /// <summary>
        /// 服务器从传输接收数据
        /// </summary>
        internal void OnServerReceive(int clientId, ArraySegment<byte> segment, Channel channel)
        {
            if (!clients.TryGetValue(clientId, out var client))
            {
                Debug.LogError($"服务器接收到消息。未知的客户端：{clientId}");
                return;
            }

            if (!client.readerPack.ReadEnqueue(segment))
            {
                Debug.LogWarning($"无法将读取消息合批!。断开客户端：{client}");
                client.Disconnect();
                return;
            }

            while (!isLoadScene && client.readerPack.ReadDequeue(out var reader, out double remoteTime))
            {
                if (reader.Residue < NetworkConst.MessageSize)
                {
                    Debug.LogError($"网络消息应该有个开始的Id。断开客户端：{client}");
                    client.Disconnect();
                    return;
                }

                client.remoteTime = remoteTime;

                if (!NetworkMessage.ReadMessage(reader, out ushort id))
                {
                    Debug.LogError($"无效的网络消息类型！断开客户端：{client}");
                    client.Disconnect();
                    return;
                }

                if (!messages.TryGetValue(id, out MessageDelegate handle))
                {
                    Debug.LogError($"未知的网络消息Id：{id} 断开客户端：{client}");
                    client.Disconnect();
                    return;
                }

                handle.Invoke(client, reader, channel);
            }

            if (!isLoadScene && client.readerPack.Count > 0)
            {
                Debug.LogError($"读取器合批之后仍然还有次数残留！残留次数：{client.readerPack.Count}");
            }
        }
    }

    public partial class ServerManager
    {
        /// <summary>
        /// 生成物体
        /// </summary>
        internal void SpawnObjects()
        {
            if (!isActive)
            {
                Debug.LogError($"服务器不是活跃的。");
                return;
            }

            var objects = Resources.FindObjectsOfTypeAll<NetworkObject>();
            foreach (var @object in objects)
            {
                if (NetworkUtils.IsSceneObject(@object) && @object.objectId == 0)
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

            if (NetworkManager.Instance.mode == NetworkMode.Host)
            {
                if (@object.connection?.clientId == NetworkConst.HostId)
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

            SpawnForClient(@object);
        }

        /// <summary>
        /// 遍历所有客户端，发送生成物体的消息
        /// </summary>
        /// <param name="object">传入对象</param>
        private void SpawnForClient(NetworkObject @object)
        {
            foreach (var client in clients.Values.Where(client => client.isReady))
            {
                SendSpawnMessage(client, @object);
            }
        }

        /// <summary>
        /// 服务器向指定客户端发送生成对象的消息
        /// </summary>
        /// <param name="client">指定的客户端</param>
        /// <param name="object">生成的游戏对象</param>
        private void SendSpawnMessage(NetworkClient client, NetworkObject @object)
        {
            using NetworkWriter owner = NetworkWriter.Pop(), observer = NetworkWriter.Pop();
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
                segment = SerializeObject(@object, isOwner, owner, observer)
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
            if (@object.entities.Length == 0) return default;
            @object.ServerSerialize(true, owner, observer);
            return isOwner ? owner.ToArraySegment() : observer.ToArraySegment();
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
                Debug.Log($"服务器为客户端 {client.clientId} 重置 {@object}");
                client.Send(new DespawnMessage(@object.objectId));
            }

            if (NetworkManager.Instance.mode == NetworkMode.Host)
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

            if (NetworkManager.Instance.mode == NetworkMode.Host)
            {
                @object.OnStopClient();
                @object.isOwner = false;
                @object.OnNotifyAuthority();
                NetworkManager.Client.spawns.Remove(@object.objectId);
            }

            @object.OnStopServer();
            Destroy(@object.gameObject);
        }
    }

    public partial class ServerManager
    {
        /// <summary>
        /// 在Update之前调用
        /// </summary>
        internal void EarlyUpdate()
        {
            if (NetworkManager.Transport != null)
            {
                NetworkManager.Transport.ServerEarlyUpdate();
            }

            foreach (var client in clients.Values)
            {
                client.UpdateInterpolation();
            }
        }

        /// <summary>
        /// 在Update之后调用
        /// </summary>
        internal void AfterUpdate()
        {
            if (isActive)
            {
                if (NetworkUtils.HeartBeat(NetworkManager.Time.localTime, NetworkManager.Instance.sendRate, ref sendTime))
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
                    client.Send(new SnapshotMessage(), Channel.Unreliable);
                    BroadcastToClient(client);
                }

                client.OnUpdate();
            }
        }

        /// <summary>
        /// 被广播的指定客户端
        /// </summary>
        /// <param name="client">指定的客户端</param>
        private void BroadcastToClient(NetworkClient client)
        {
            foreach (var @object in spawns.Values)
            {
                if (@object != null)
                {
                    NetworkWriter writer = SerializeForClient(@object, client);
                    if (writer != null)
                    {
                        client.Send(new EntityMessage(@object.objectId, writer.ToArraySegment()));
                    }
                }
                else
                {
                    Debug.LogWarning($"在观察列表中为 {client.clientId} 找到了空对象。请用NetworkServer.Destroy");
                }
            }
        }

        /// <summary>
        /// 为客户端序列化 SyncVar
        /// </summary>
        /// <param name="object"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        private NetworkWriter SerializeForClient(NetworkObject @object, NetworkClient client)
        {
            var serialize = @object.ServerSerializeTick(Time.frameCount);

            if (@object.connection == client)
            {
                if (serialize.owner.position > 0)
                {
                    return serialize.owner;
                }
            }
            else
            {
                if (serialize.observer.position > 0)
                {
                    return serialize.observer;
                }
            }

            return null;
        }
    }
}