using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    public partial class ClientManager : Component<NetworkManager>
    {
        /// <summary>
        /// 网络消息委托字典
        /// </summary>
        [ShowInInspector] internal readonly Dictionary<ushort, MessageDelegate> messages = new Dictionary<ushort, MessageDelegate>();

        /// <summary>
        /// 场景中包含的网络对象
        /// </summary>
        [ShowInInspector] internal readonly Dictionary<ulong, NetworkObject> scenes = new Dictionary<ulong, NetworkObject>();

        /// <summary>
        /// 客户端生成的网络对象
        /// </summary>
        [ShowInInspector] internal readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();

        /// <summary>
        /// 连接的状态
        /// </summary>
        [ShowInInspector] private ConnectState state;

        /// <summary>
        /// 上一次发送信息的时间
        /// </summary>
        [ShowInInspector] private double sendTime;

        /// <summary>
        /// 是否在生成物体中
        /// </summary>
        [ShowInInspector] private bool isSpawning;

        /// <summary>
        /// 是否已经准备完成(能进行和Server的信息传输)
        /// </summary>
        [ShowInInspector]
        public bool isReady { get; internal set; }

        /// <summary>
        /// 是否正在加载场景
        /// </summary>
        [ShowInInspector]
        public bool isLoadScene { get; internal set; }

        /// <summary>
        /// 连接到的服务器
        /// </summary>
        [ShowInInspector]
        public NetworkServer connection { get; private set; }

        /// <summary>
        /// 是否活跃
        /// </summary>
        [ShowInInspector]
        public bool isActive => state is ConnectState.Connected or ConnectState.Connecting;

        /// <summary>
        /// 是否已经连接成功
        /// </summary>
        [ShowInInspector]
        public bool isAuthority => state == ConnectState.Connected;

        /// <summary>
        /// 客户端连接的事件(包含主机)
        /// </summary>
        public event Action OnConnect;

        /// <summary>
        /// 客户端断开的事件
        /// </summary>
        public event Action OnDisconnect;

        /// <summary>
        /// 客户端取消准备的事件
        /// </summary>
        public event Action OnNotReady;

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="uri">传入Uri</param>
        internal void StartClient(Uri uri)
        {
            RegisterTransport();
            Register(false);
            state = ConnectState.Connecting;
            NetworkManager.Transport.ClientConnect(uri);
            connection = new NetworkServer();
        }

        /// <summary>
        /// 开启主机，使用Server的Transport
        /// </summary>
        internal void StartClient()
        {
            Register(true);
            state = ConnectState.Connected;
            connection = new NetworkServer();
            var client = new NetworkClient(NetworkConst.HostId);
            NetworkManager.Server.OnClientConnect(client);
            Ready();
        }

        /// <summary>
        /// 设置客户端准备(能够进行消息传输)
        /// </summary>
        public void Ready()
        {
            if (connection == null)
            {
                Debug.LogError("没有连接到有效的服务器！");
                return;
            }
            
            if (isReady)
            {
                Debug.LogError("客户端已经准备就绪！");
                return;
            }

            isReady = true;
            connection.isReady = true;
            connection.Send(new SetReadyMessage());
        }

        /// <summary>
        /// 客户端发送消息到服务器 (对发送消息的封装)
        /// </summary>
        /// <param name="message">网络事件</param>
        /// <param name="channel">传输通道</param>
        /// <typeparam name="T"></typeparam>
        internal void Send<T>(T message, Channel channel = Channel.Reliable) where T : struct, Message
        {
            if (connection == null)
            {
                Debug.LogError("没有连接到有效的服务器！");
                return;
            }

            if (state != ConnectState.Connected)
            {
                Debug.LogError("客户端没有连接成功就向服务器发送消息！");
                return;
            }

            connection.Send(message, channel);
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        internal void StopClient()
        {
            if (!isActive) return;
            Debug.Log("停止客户端。");
            foreach (var @object in spawns.Values.Where(@object => @object != null))
            {
                if (NetworkManager.Instance.mode != NetworkMode.Server)
                {
                    @object.OnStopClient();
                    if (@object.sceneId != 0)
                    {
                        @object.gameObject.SetActive(false);
                        @object.Reset();
                    }
                    else
                    {
                        Destroy(@object.gameObject);
                    }
                }
            }

            state = ConnectState.Disconnected;
            if (NetworkManager.Transport != null)
            {
                NetworkManager.Transport.ClientDisconnect();
            }

            OnDisconnect?.Invoke();
            spawns.Clear();
            scenes.Clear();
            messages.Clear();
            sendTime = 0;
            isReady = false;
            connection = null;
            isLoadScene = false;
        }
    }

    public partial class ClientManager
    {
        /// <summary>
        /// 注册网络消息
        /// </summary>
        /// <param name="isHost">是否是基于主机的连接</param>
        private void Register(bool isHost)
        {
            if (isHost)
            {
                Register<SpawnMessage>(OnSpawnByHost);
                Register<DestroyMessage>(OnEmptyByHost);
                Register<DespawnMessage>(OnEmptyByHost);
                Register<PongMessage>(OnEmptyByHost);
                Register<EntityMessage>(OnEmptyByHost);
            }
            else
            {
                Register<SpawnMessage>(OnSpawnByClient);
                Register<DestroyMessage>(OnDestroyByClient);
                Register<DespawnMessage>(OnDespawnByClient);
                Register<PongMessage>(OnPongByClient);
                Register<EntityMessage>(OnEntityEvent);
            }

            Register<NotReadyMessage>(OnNotReadyByClient);
            Register<SceneMessage>(OnSceneByClient);
            Register<SnapshotMessage>(OnSnapshotByClient);
            Register<InvokeRpcMessage>(OnInvokeRpcByClient);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private void Register<TMessage>(Action<TMessage> handle) where TMessage : struct, Message
        {
            messages[NetworkMessage<TMessage>.Id] = NetworkMessage.Register(handle);
        }

        /// <summary>
        /// 主机模式下空的网络消息
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        private void OnEmptyByHost<T>(T message) where T : Message
        {
        }

        /// <summary>
        /// 主机模式下生成物体的消息
        /// </summary>
        /// <param name="message"></param>
        private void OnSpawnByHost(SpawnMessage message)
        {
            if (NetworkManager.Server.spawns.TryGetValue(message.objectId, out var @object))
            {
                spawns[message.objectId] = @object;
                @object.gameObject.SetActive(true);
                @object.isOwner = message.isOwner;
                @object.isClient = true;
                @object.OnStartClient();
                @object.OnNotifyAuthority();
            }
        }

        /// <summary>
        /// 客户端下隐藏物体的消息
        /// </summary>
        /// <param name="message"></param>
        private void OnDespawnByClient(DespawnMessage message)
        {
            if (spawns.TryGetValue(message.objectId, out var @object))
            {
                @object.OnStopClient();
                @object.gameObject.SetActive(false);
                @object.Reset();
                spawns.Remove(message.objectId);
            }
        }

        /// <summary>
        /// 客户端下销毁物体的消息
        /// </summary>
        /// <param name="message"></param>
        private void OnDestroyByClient(DestroyMessage message)
        {
            if (spawns.TryGetValue(message.objectId, out var @object))
            {
                @object.OnStopClient();
                Destroy(@object.gameObject);
                spawns.Remove(message.objectId);
            }
        }

        /// <summary>
        /// 客户端下生成物体的消息
        /// </summary>
        /// <param name="message"></param>
        private void OnSpawnByClient(SpawnMessage message)
        {
            scenes.Clear();
            var objects = Resources.FindObjectsOfTypeAll<NetworkObject>();
            foreach (var obj in objects)
            {
                if (!NetworkUtils.IsSceneObject(obj)) continue;
                if (scenes.TryGetValue(obj.sceneId, out var o))
                {
                    var gameObject = obj.gameObject;
                    Debug.LogWarning($"复制 {gameObject.name} 到 {o.gameObject.name} 上检测到 sceneId", gameObject);
                }
                else
                {
                    scenes.Add(obj.sceneId, obj);
                }
            }

            SpawnObject(message);
        }

        /// <summary>
        /// 接收 远程过程调用(RPC) 缓存的消息
        /// </summary>
        /// <param name="message"></param>
        private void OnInvokeRpcByClient(InvokeRpcMessage message)
        {
            using var reader = NetworkReader.Pop(message.segment);
            while (reader.Residue > 0)
            {
                var clientRpc = reader.Read<ClientRpcMessage>();
                OnClientRpcEvent(clientRpc);
            }
        }

        /// <summary>
        /// 当接收到 ClientRpc 的消息
        /// </summary>
        /// <param name="message"></param>
        private void OnClientRpcEvent(ClientRpcMessage message)
        {
            if (!spawns.TryGetValue(message.objectId, out var @object)) return;
            using var reader = NetworkReader.Pop(message.segment);
            @object.InvokeRpcMessage(message.serialId, message.methodHash, RpcType.ClientRpc, reader);
        }
        
        /// <summary>
        /// 当接收到服务器的 Pong 消息
        /// </summary>
        /// <param name="message"></param>
        private void OnPongByClient(PongMessage message)
        {
            NetworkManager.Time.Ping(message.clientTime);
        }

        /// <summary>
        /// 客户端下网络消息快照的消息
        /// </summary>
        /// <param name="message"></param>
        private void OnSnapshotByClient(SnapshotMessage message)
        {
            connection.OnSnapshotMessage(new SnapshotTime(connection.remoteTime, NetworkManager.Time.localTime));
        }

        /// <summary>
        /// 实体状态同步
        /// </summary>
        /// <param name="message"></param>
        private void OnEntityEvent(EntityMessage message)
        {
            if (spawns.TryGetValue(message.objectId, out var @object) && @object != null)
            {
                using var reader = NetworkReader.Pop(message.segment);
                @object.ClientDeserialize(reader, false);
            }
            else
            {
                Debug.LogWarning($"没有为 {message.objectId} 的同步消息找到目标。");
            }
        }

        /// <summary>
        /// 客户端场景改变
        /// </summary>
        /// <param name="message"></param>
        private void OnSceneByClient(SceneMessage message)
        {
            if (isAuthority)
            {
                NetworkManager.Scene.ClientLoadScene(message.sceneName);
            }
        }

        /// <summary>
        /// 客户端未准备就绪的消息 (不能接收和发送消息)
        /// </summary>
        /// <param name="message"></param>
        private void OnNotReadyByClient(NotReadyMessage message)
        {
            isReady = false;
            OnNotReady?.Invoke();
        }
    }

    public partial class ClientManager
    {
        /// <summary>
        /// 添加传输事件
        /// </summary>
        private void RegisterTransport()
        {
            NetworkManager.Transport.OnClientConnected -= OnClientConnected;
            NetworkManager.Transport.OnClientDisconnected -= OnClientDisconnected;
            NetworkManager.Transport.OnClientReceive -= OnClientReceive;
            NetworkManager.Transport.OnClientConnected += OnClientConnected;
            NetworkManager.Transport.OnClientDisconnected += OnClientDisconnected;
            NetworkManager.Transport.OnClientReceive += OnClientReceive;
        }

        /// <summary>
        /// 当客户端连接
        /// </summary>
        private void OnClientConnected()
        {
            if (connection == null)
            {
                Debug.LogError("没有有效的服务器连接！");
                return;
            }
            
            state = ConnectState.Connected;
            OnConnect?.Invoke();
            NetworkManager.Time.Reset();
            NetworkManager.Time.Update();
            Ready();
        }

        /// <summary>
        /// 当客户端断开连接
        /// </summary>
        private void OnClientDisconnected()
        {
            StopClient();
        }

        /// <summary>
        /// 当客户端从服务器接收消息
        /// </summary>
        /// <param name="data"></param>
        /// <param name="channel"></param>
        internal void OnClientReceive(ArraySegment<byte> data, Channel channel)
        {
            if (connection == null)
            {
                Debug.LogError("没有连接到有效的服务器！");
                return;
            }

            if (!connection.readerBatch.ReadEnqueue(data))
            {
                Debug.LogError($"无法将读取消息合批!");
                connection.Disconnect();
                return;
            }

            while (!isLoadScene && connection.readerBatch.ReadDequeue(out var reader, out double remoteTime))
            {
                if (reader.Residue < NetworkConst.MessageSize)
                {
                    Debug.LogError($"网络消息应该有个开始的Id");
                    connection.Disconnect();
                    return;
                }

                connection.remoteTime = remoteTime;

                if (!NetworkMessage.ReadMessage(reader, out ushort id))
                {
                    Debug.LogError("无效的网络消息类型！");
                    connection.Disconnect();
                    return;
                }

                if (!messages.TryGetValue(id, out MessageDelegate handle))
                {
                    Debug.LogError($"未知的网络消息Id：{id}");
                    connection.Disconnect();
                    return;
                }

                handle.Invoke(connection, reader, channel);
            }

            if (!isLoadScene && connection.readerBatch.Count > 0)
            {
                Debug.LogError($"读取器合批之后仍然还有次数残留！残留次数：{connection.readerBatch.Count}\n");
            }
        }
    }

    public partial class ClientManager
    {
        /// <summary>
        /// 获取网络对象
        /// </summary>
        /// <param name="message">传入网络消息</param>
        /// <returns>返回是否能获取</returns>
        private async void SpawnObject(SpawnMessage message)
        {
            if (spawns.TryGetValue(message.objectId, out var @object))
            {
                isSpawning = false;
                Spawn(message, @object);
                SpawnFinish();
                isSpawning = true;
                return;
            }

            if (message.sceneId == 0)
            {
                var prefab = await GlobalManager.Asset.Load<GameObject>(message.assetId);
                if (!prefab.TryGetComponent(out @object))
                {
                    Debug.LogError($"预置体 {prefab.name} 没有 NetworkObject 组件");
                    return;
                }

                if (@object.sceneId != 0)
                {
                    Debug.LogError($"不能注册预置体 {@object.name} 因为 sceneId 不为零");
                    return;
                }

                if (@object.GetComponentsInChildren<NetworkObject>().Length > 1)
                {
                    Debug.LogError($"不能注册预置体 {@object.name} 因为它拥有多个 NetworkObject 组件");
                }

                isSpawning = false;
                Spawn(message, @object);
                SpawnFinish();
                isSpawning = true;
            }
            else
            {
                if (!scenes.Remove(message.sceneId, out @object))
                {
                    Debug.LogError($"无法生成有效场景对象。 sceneId：{message.sceneId}");
                    return;
                }

                isSpawning = false;
                Spawn(message, @object);
                SpawnFinish();
                isSpawning = true;
            }
        }

        /// <summary>
        /// 生成网络对象
        /// </summary>
        /// <param name="object"></param>
        /// <param name="message"></param>
        private void Spawn(SpawnMessage message, NetworkObject @object)
        {
            if (!@object.gameObject.activeSelf)
            {
                @object.gameObject.SetActive(true);
            }
            
            @object.objectId = message.objectId;
            @object.isOwner = message.isOwner;
            @object.isClient = true;

            var transform = @object.transform;
            transform.localPosition = message.position;
            transform.localRotation = message.rotation;
            transform.localScale = message.localScale;

            if (message.segment.Count > 0)
            {
                using var reader = NetworkReader.Pop(message.segment);
                @object.ClientDeserialize(reader, true);
            }

            spawns[message.objectId] = @object;
            if (isSpawning)
            {
                @object.OnNotifyAuthority();
                @object.OnStartClient();
            }
        }

        /// <summary>
        /// 网络对象生成结束
        /// </summary>
        private void SpawnFinish()
        {
            foreach (var @object in spawns.Values)
            {
                if (@object == null)
                {
                    Debug.LogWarning($"网络对象 {@object} 没有被正确销毁。");
                    continue;
                }

                @object.isClient = true;
                @object.OnNotifyAuthority();
                @object.OnStartClient();
            }
        }
    }

    public partial class ClientManager
    {
        /// <summary>
        /// 在Update前调用
        /// </summary>
        internal void EarlyUpdate()
        {
            if (NetworkManager.Transport != null)
            {
                NetworkManager.Transport.ClientEarlyUpdate();
            }

            connection?.UpdateInterpolation();
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

            if (connection != null)
            {
                if (NetworkManager.Instance.mode == NetworkMode.Host)
                {
                    connection.OnUpdate();
                }
                else
                {
                    if (isActive && isAuthority)
                    {
                        NetworkManager.Time.Update();
                        connection.OnUpdate();
                    }
                }
            }

            if (NetworkManager.Transport != null)
            {
                NetworkManager.Transport.ClientAfterUpdate();
            }
        }

        /// <summary>
        /// 客户端进行广播
        /// </summary>
        private void Broadcast()
        {
            if (!connection.isReady) return;
            if (NetworkManager.Server.isActive) return;
            foreach (var @object in spawns.Values)
            {
                using var writer = NetworkWriter.Pop();
                @object.ClientSerialize(writer);
                if (writer.position > 0)
                {
                    Send(new EntityMessage(@object.objectId, writer.ToArraySegment()));
                    @object.ClearDirty();
                }
            }

            Send(new SnapshotMessage(), Channel.Unreliable);
        }
    }
}