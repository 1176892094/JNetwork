using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    public partial class NetworkManager
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
            /// <param name="address">传入连接地址</param>
            /// <param name="port">传入连接端口</param>
            internal void StartClient(string address, ushort port)
            {
                RegisterTransport();
                Register(false);
                state = ConnectState.Connecting;
                Transport.current.ClientConnect(address, port);
                connection = new NetworkServer();
            }

            /// <summary>
            /// 开启客户端
            /// </summary>
            /// <param name="uri">传入Uri</param>
            internal void StartClient(Uri uri)
            {
                RegisterTransport();
                Register(false);
                state = ConnectState.Connecting;
                Transport.current.ClientConnect(uri);
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
                Server.OnClientConnect(client);
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
                state = ConnectState.Disconnected;
                foreach (var @object in spawns.Values.Where(@object => @object != null))
                {
                    if (Instance.mode is NetworkMode.Client)
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

                if (Transport.current != null)
                {
                    Transport.current.ClientDisconnect();
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

            /// <summary>
            /// 清除事件
            /// </summary>
            private void OnDestroy()
            {
                OnConnect = null;
                OnDisconnect = null;
                OnNotReady = null;
            }
        }
    }
}