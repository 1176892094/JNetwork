using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    public partial class NetworkManager
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
            private readonly List<NetworkClient> copies = new List<NetworkClient>();

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
            /// 连接到的主机客户端
            /// </summary>
            [ShowInInspector]
            public NetworkClient connection { get; internal set; }

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
                    Transport.current.StartServer();
                }

                if (!isActive)
                {
                    isActive = true;
                    clients.Clear();
                    Register();
                    RegisterTransport();
                    Time.Reset();
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
                    Debug.Log($"设置客户端 {client.clientId} 取消准备");
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
                var copies = clients.Values.ToList();
                foreach (var client in copies)
                {
                    client.Disconnect();
                    if (client.clientId != NetworkConst.HostId)
                    {
                        OnServerDisconnected(client.clientId);
                    }
                }

                if (Transport.current != null)
                {
                    Transport.current.StopServer();
                }

                UnRegisterTransport();
                spawns.Clear();
                clients.Clear();
                messages.Clear();
                sendTime = 0;
                objectId = 0;
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
                OnSetReady = null;
            }
        }
    }
}