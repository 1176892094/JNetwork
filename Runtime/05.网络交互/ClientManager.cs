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
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    using MessageDelegate = Action<NetworkClient, NetworkReader, int>;

    public partial class ClientManager : Controller<NetworkManager>
    {
        /// <summary>
        /// 网络消息委托字典
        /// </summary>
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        internal readonly Dictionary<ushort, MessageDelegate> messages = new Dictionary<ushort, MessageDelegate>();

        /// <summary>
        /// 场景中包含的网络对象
        /// </summary>
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        internal readonly Dictionary<ulong, NetworkObject> scenes = new Dictionary<ulong, NetworkObject>();

        /// <summary>
        /// 客户端生成的网络对象
        /// </summary>
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        internal readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();

        /// <summary>
        /// 连接的状态
        /// </summary>
        private StateMode state = StateMode.Disconnect;

        /// <summary>
        /// 上一次发送信息的时间
        /// </summary>
        [SerializeField] private double sendTime;

        /// <summary>
        /// 客户端发送 Ping 的间隔
        /// </summary>
        [SerializeField] private double waitTime;

        /// <summary>
        /// 客户端回传 Ping 时间
        /// </summary>
        [SerializeField] private double pingTime;

        /// <summary>
        /// 是否活跃
        /// </summary>
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        public bool isActive => state != StateMode.Disconnect;

        /// <summary>
        /// 是否已经准备完成(能进行和Server的信息传输)
        /// </summary>
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        public bool isReady { get; internal set; }

        /// <summary>
        /// 是否正在加载场景
        /// </summary>
        public bool isLoadScene { get; internal set; }

        /// <summary>
        /// 连接到的服务器
        /// </summary>
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        public NetworkServer connection { get; private set; }

        /// <summary>
        /// 是否已经连接成功
        /// </summary>
        public bool isConnected => state == StateMode.Connected;

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
        /// 客户端加载场景的事件
        /// </summary>
        public event Action<string> OnLoadScene;

        /// <summary>
        /// 客户端加载场景完成的事件
        /// </summary>
        public event Action<string> OnLoadComplete;

        /// <summary>
        /// 开启主机，使用Server的Transport
        /// </summary>
        /// <param name="mode"></param>
        internal void StartClient(EntryMode mode)
        {
            if (mode == EntryMode.Host)
            {
                Register(EntryMode.Host);
                state = StateMode.Connected;
                connection = new NetworkServer();
                NetworkManager.Server.Connect(new NetworkClient(Const.HostId));
                Ready();
                return;
            }

            Register(EntryMode.Client);
            state = StateMode.Connect;
            connection = new NetworkServer();
            NetworkManager.Transport.StartClient();
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="uri">传入Uri</param>
        internal void StartClient(Uri uri)
        {
            Register(EntryMode.Client);
            state = StateMode.Connect;
            connection = new NetworkServer();
            NetworkManager.Transport.StartClient(uri);
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        internal void StopClient()
        {
            if (!isActive) return;
            if (NetworkManager.Mode != EntryMode.Server)
            {
                var copies = spawns.Values.Where(@object => @object != null).ToList();
                foreach (var @object in copies)
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

            state = StateMode.Disconnect;
            if (NetworkManager.Transport != null)
            {
                NetworkManager.Transport.StopClient();
            }

            sendTime = 0;
            waitTime = 0;
            pingTime = 0;
            isReady = false;
            spawns.Clear();
            scenes.Clear();
            messages.Clear();
            connection = null;
            isLoadScene = false;
            OnDisconnect?.Invoke();
        }

        /// <summary>
        /// 客户端发送 Ping 时间
        /// </summary>
        private void Pong()
        {
            if (waitTime + 2 <= Time.unscaledTimeAsDouble)
            {
                waitTime = Time.unscaledTimeAsDouble;
                connection.Send(new PongMessage(waitTime), Channel.Unreliable);
            }
        }

        /// <summary>
        /// 客户端发送 Ready
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
            connection.Send(new ReadyMessage());
        }

        /// <summary>
        /// 客户端加载场景
        /// </summary>
        /// <param name="sceneName"></param>
        private async void Load(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("客户端不能加载空场景！");
                return;
            }
            
            if (isLoadScene && NetworkManager.Instance.sceneName == sceneName)
            {
                Debug.LogError($"客户端正在加载 {sceneName} 场景");
                return;
            }

            OnLoadScene?.Invoke(sceneName);
            if (NetworkManager.Server.isActive) return;
            isLoadScene = true;
            NetworkManager.Instance.sceneName = sceneName;
            
            await AssetManager.LoadScene(sceneName);
            NetworkManager.Instance.OnLoadComplete();
        }

        /// <summary>
        /// 客户端场景加载完成
        /// </summary>
        internal void LoadSceneComplete(string sceneName)
        {
            isLoadScene = false;
            if (isConnected && !isReady)
            {
                Ready();
            }

            OnLoadComplete?.Invoke(sceneName);
        }
    }

    public partial class ClientManager
    {
        /// <summary>
        /// 注册客户端消息
        /// </summary>
        /// <param name="mode"></param>
        private void Register(EntryMode mode)
        {
            if (mode == EntryMode.Client)
            {
                NetworkManager.Transport.OnClientConnect -= OnClientConnect;
                NetworkManager.Transport.OnClientDisconnect -= OnClientDisconnect;
                NetworkManager.Transport.OnClientError -= OnClientError;
                NetworkManager.Transport.OnClientReceive -= OnClientReceive;
                NetworkManager.Transport.OnClientConnect += OnClientConnect;
                NetworkManager.Transport.OnClientDisconnect += OnClientDisconnect;
                NetworkManager.Transport.OnClientError += OnClientError;
                NetworkManager.Transport.OnClientReceive += OnClientReceive;
            }

            Register<PingMessage>(PingMessage);
            Register<NotReadyMessage>(NotReadyMessage);
            Register<EntityMessage>(EntityMessage);
            Register<ClientRpcMessage>(ClientRpcMessage);

            Register<SceneMessage>(SceneMessage);
            Register<SpawnMessage>(SpawnMessage);
            Register<DespawnMessage>(DespawnMessage);
        }

        /// <summary>
        /// 注册客户端网络消息处理
        /// </summary>
        /// <param name="handle"></param>
        /// <typeparam name="T"></typeparam>
        public void Register<T>(Action<T> handle) where T : struct, IMessage
        {
            messages[Message<T>.Id] = NetworkUtility.GetMessage(handle);
        }

        /// <summary>
        /// 处理Ping网络消息
        /// </summary>
        /// <param name="message"></param>
        private void PingMessage(PingMessage message)
        {
            if (NetworkManager.Server.isActive)
            {
                return;
            }

            if (pingTime <= 0)
            {
                pingTime = Time.unscaledTimeAsDouble - message.clientTime;
            }
            else
            {
                var delta = Time.unscaledTimeAsDouble - message.clientTime - pingTime;
                pingTime += 2.0 / (6 + 1) * delta;
            }

            NetworkManager.Ping(pingTime);
        }

        /// <summary>
        /// 处理Ready网络消息
        /// </summary>
        /// <param name="message"></param>
        private void NotReadyMessage(NotReadyMessage message)
        {
            isReady = false;
            OnNotReady?.Invoke();
        }

        /// <summary>
        /// 处理Entity网络消息
        /// </summary>
        /// <param name="message"></param>
        private void EntityMessage(EntityMessage message)
        {
            if (NetworkManager.Server.isActive)
            {
                return;
            }

            if (!spawns.TryGetValue(message.objectId, out var @object))
            {
                Debug.LogWarning($"无法同步网络对象 {message.objectId}。");
                return;
            }

            if (@object == null)
            {
                Debug.LogWarning($"无法同步网络对象 {message.objectId}。");
                return;
            }

            using var reader = NetworkReader.Pop(message.segment);
            @object.ClientDeserialize(reader, false);
        }

        /// <summary>
        /// 服务器发送ClientRpc请求，客户端接受后调用
        /// </summary>
        /// <param name="message"></param>
        private void ClientRpcMessage(ClientRpcMessage message)
        {
            if (spawns.TryGetValue(message.objectId, out var @object))
            {
                using var reader = NetworkReader.Pop(message.segment);
                @object.InvokeMessage(message.componentId, message.methodHash, InvokeMode.ClientRpc, reader);
            }
        }

        /// <summary>
        /// 客户端改变场景
        /// </summary>
        /// <param name="message"></param>
        private void SceneMessage(SceneMessage message)
        {
            if (!isConnected)
            {
                Debug.LogWarning($"客户端没有通过校验，无法加载场景{message.sceneName}。");
                return;
            }

            Load(message.sceneName);
        }

        /// <summary>
        /// 接收网络对象生成的消息
        /// </summary>
        /// <param name="message"></param>
        private void SpawnMessage(SpawnMessage message)
        {
            if (NetworkManager.Server.isActive)
            {
                if (NetworkManager.Server.spawns.TryGetValue(message.objectId, out var @object))
                {
                    spawns[message.objectId] = @object;
                    @object.gameObject.SetActive(true);
                    if (message.isOwner)
                    {
                        @object.entityMode |= EntityMode.Owner;
                    }
                    else
                    {
                        @object.entityMode &= ~EntityMode.Owner;
                    }

                    @object.entityMode |= EntityMode.Client;
                    @object.OnStartClient();
                    @object.OnNotifyAuthority();
                }

                return;
            }

            scenes.Clear();
            var objects = Resources.FindObjectsOfTypeAll<NetworkObject>();
            foreach (var @object in objects)
            {
                if (NetworkUtility.IsSceneObject(@object))
                {
                    if (scenes.TryGetValue(@object.sceneId, out var obj))
                    {
                        Debug.LogWarning($"场景Id存在重复。网络对象：{@object.name}  {obj.name}");
                        continue;
                    }

                    scenes.Add(@object.sceneId, @object);
                }
            }

            SpawnObject(message);
        }

        /// <summary>
        /// 接收网络对象销毁的消息
        /// </summary>
        /// <param name="message"></param>
        private void DespawnMessage(DespawnMessage message)
        {
            if (!spawns.TryGetValue(message.objectId, out var @object))
            {
                return;
            }

            @object.OnStopClient();
            @object.entityMode &= ~EntityMode.Owner;
            @object.OnNotifyAuthority();
            spawns.Remove(message.objectId);

            if (NetworkManager.Server.isActive)
            {
                return;
            }

            if (@object.assetId.Equals(@object.name, StringComparison.OrdinalIgnoreCase))
            {
                PoolManager.Push(@object.gameObject);
                @object.Reset();
                return;
            }

            Destroy(@object.gameObject);
        }
    }

    public partial class ClientManager
    {
        /// <summary>
        /// 当客户端连接
        /// </summary>
        private void OnClientConnect()
        {
            if (connection == null)
            {
                Debug.LogError("没有有效的服务器连接！");
                return;
            }

            state = StateMode.Connected;
            OnConnect?.Invoke();
            Pong();
            Ready();
        }

        /// <summary>
        /// 当客户端断开连接
        /// </summary>
        private void OnClientDisconnect()
        {
            StopClient();
        }

        /// <summary>
        /// 当客户端发生错误
        /// </summary>
        /// <param name="error"></param>
        /// <param name="message"></param>
        private void OnClientError(int error, string message)
        {
            var reason = error switch
            {
                1 => "DnsResolve",
                2 => "Timeout",
                3 => "Congestion",
                4 => "InvalidReceive",
                5 => "InvalidSend",
                6 => "ConnectionClosed",
                _ => "Unexpected",
            };

            Debug.LogWarning($"错误代码：{reason} => {message}");
        }

        /// <summary>
        /// 当客户端从服务器接收消息
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="channel"></param>
        internal void OnClientReceive(ArraySegment<byte> segment, int channel)
        {
            if (connection == null)
            {
                Debug.LogError("没有连接到有效的服务器！");
                return;
            }

            if (!connection.reader.AddBatch(segment))
            {
                Debug.LogWarning($"无法将消息写入。");
                connection.Disconnect();
                return;
            }

            while (!isLoadScene && connection.reader.GetMessage(out var newSeg, out var remoteTime))
            {
                using var reader = NetworkReader.Pop(newSeg);
                if (reader.residue < sizeof(ushort))
                {
                    Debug.LogError($"网络消息应该有个开始的Id。");
                    connection.Disconnect();
                    return;
                }

                var message = reader.ReadUShort();
                if (!messages.TryGetValue(message, out var action))
                {
                    Debug.LogError($"未知的网络消息Id：{message}");
                    connection.Disconnect();
                    return;
                }

                connection.remoteTime = remoteTime;
                action.Invoke(null, reader, channel);
            }

            if (!isLoadScene && connection.reader.Count > 0)
            {
                Debug.LogError($"有残留消息没被写入！残留数：{connection.reader.Count}\n");
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
                Spawn(message, @object);
                return;
            }

            if (message.sceneId == 0)
            {
                GameObject prefab;
                if (message.isPool)
                {
                    prefab = await PoolManager.Pop(message.assetId);
                }
                else
                {
                    prefab = await AssetManager.Load<GameObject>(message.assetId);
                }

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
                    return;
                }
            }
            else
            {
                if (!scenes.Remove(message.sceneId, out @object))
                {
                    Debug.LogError($"无法生成有效场景对象。 sceneId：{message.sceneId}");
                    return;
                }
            }

            Spawn(message, @object);
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
            if (message.isOwner)
            {
                @object.entityMode |= EntityMode.Owner;
            }
            else
            {
                @object.entityMode &= ~EntityMode.Owner;
            }

            @object.entityMode |= EntityMode.Client;
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
            @object.OnNotifyAuthority();
            @object.OnStartClient();
        }
    }

    public partial class ClientManager
    {
        /// <summary>
        /// 在Update更新之前
        /// </summary>
        internal void EarlyUpdate()
        {
            if (NetworkManager.Transport != null)
            {
                NetworkManager.Transport.ClientEarlyUpdate();
            }
        }

        /// <summary>
        /// 在Update更新之后
        /// </summary>
        internal void AfterUpdate()
        {
            if (isActive)
            {
                if (NetworkManager.Instance.Tick(ref sendTime))
                {
                    Broadcast();
                }
            }

            if (connection != null)
            {
                if (NetworkManager.Mode == EntryMode.Host)
                {
                    connection.Update();
                }
                else
                {
                    if (isConnected)
                    {
                        Pong();
                        connection.Update();
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
            if (NetworkManager.Server.isActive)
            {
                return;
            }

            if (!connection.isReady)
            {
                return;
            }

            foreach (var @object in spawns.Values)
            {
                using var writer = NetworkWriter.Pop();
                @object.ClientSerialize(writer);
                if (writer.position > 0)
                {
                    connection.Send(new EntityMessage(@object.objectId, writer));
                    @object.ClearDirty();
                }
            }
        }
    }
}