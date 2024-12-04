// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-04  22:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using JFramework.Interface;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JFramework.Net
{
    public partial class NetworkManager : MonoBehaviour, IEntity
    {
        /// <summary>
        /// NetworkManager 单例
        /// </summary>
        public static NetworkManager Instance;

        /// <summary>
        /// 网络传输组件
        /// </summary>
        [SerializeField] private Transport transport;

        /// <summary>
        /// 网络发现组件
        /// </summary>
        [SerializeField] private NetworkDiscovery discovery;

        /// <summary>
        /// 客户端组件
        /// </summary>
        [SerializeField, Inject] private ClientManager client;

        /// <summary>
        /// 服务器组件
        /// </summary>
        [SerializeField, Inject] private ServerManager server;

        /// <summary>
        /// 心跳传输率
        /// </summary>
        [SerializeField, Range(30, 120)] private int sendRate = 30;

        /// <summary>
        /// 客户端最大连接数量
        /// </summary>
        public int connection = 100;
        
        /// <summary>
        /// 服务器场景
        /// </summary>
        internal string sceneName;

        /// <summary>
        /// 客户端组件
        /// </summary>
        public static ClientManager Client => Instance.client;

        /// <summary>
        /// 服务器组件
        /// </summary>
        public static ServerManager Server => Instance.server;

        /// <summary>
        /// 网络发现组件
        /// </summary>
        public static NetworkDiscovery Discovery => Instance.discovery;

        /// <summary>
        /// 当Ping更新
        /// </summary>
        public static event Action<double> OnPingUpdate;

        /// <summary>
        /// 网络传输组件
        /// </summary>
        public static Transport Transport
        {
            get => Instance.transport;
            set => Instance.transport = value;
        }

        /// <summary>
        /// 游戏模式
        /// </summary>
        public static EntryMode Mode
        {
            get
            {
                if (!Application.isPlaying)
                {
                    return EntryMode.None;
                }

                if (Server.isActive)
                {
                    return Client.isActive ? EntryMode.Host : EntryMode.Server;
                }

                return Client.isActive ? EntryMode.Client : EntryMode.None;
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            this.Inject();
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
        }

        /// <summary>
        /// 销毁
        /// </summary>
        private void OnDestroy()
        {
            this.Destroy();
            OnPingUpdate = null;
        }

        /// <summary>
        /// 当程序退出，停止服务器和客户端
        /// </summary>
        private void OnApplicationQuit()
        {
            if (Client.isConnected)
            {
                StopClient();
            }

            if (Server.isActive)
            {
                StopServer();
            }
        }
    }

    public partial class NetworkManager
    {
        /// <summary>
        /// 开启服务器
        /// </summary>
        public static void StartServer()
        {
            if (Server.isActive)
            {
                Debug.LogWarning("服务器已经连接！");
                return;
            }
            
            Server.StartServer(EntryMode.Server);
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public static void StopServer()
        {
            if (!Server.isActive)
            {
                Debug.LogWarning("服务器已经停止！");
                return;
            }
            
            Server.StopServer();
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        public static void StartClient()
        {
            if (Client.isActive)
            {
                Debug.LogWarning("客户端已经连接！");
                return;
            }
            
            Client.StartClient(EntryMode.Client);
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="uri"></param>
        public static void StartClient(Uri uri)
        {
            if (Client.isActive)
            {
                Debug.LogWarning("客户端已经连接！");
                return;
            }
            
            Client.StartClient(uri);
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        public static void StopClient()
        {
            if (!Client.isActive)
            {
                Debug.LogWarning("客户端已经停止！");
                return;
            }

            if (Mode == EntryMode.Host)
            {
                Server.OnServerDisconnect(Const.HostId);
            }
            
            Client.StopClient();
        }

        /// <summary>
        /// 开启主机
        /// </summary>
        public static void StartHost(EntryMode mode = EntryMode.Host)
        {
            if (Server.isActive || Client.isActive)
            {
                Debug.LogWarning("客户端或服务器已经连接！");
                return;
            }
            
            Server.StartServer(mode);
            Client.StartClient(EntryMode.Host);
        }

        /// <summary>
        /// 停止主机
        /// </summary>
        public static void StopHost()
        {
            StopClient();
            StopServer();
        }

        /// <summary>
        /// 计算发送间隔
        /// </summary>
        /// <param name="sendTime"></param>
        /// <returns></returns>
        internal bool Tick(ref double sendTime)
        {
            var duration = 1.0 / sendRate;
            if (sendTime + duration <= Time.unscaledTimeAsDouble)
            {
                sendTime = (long)(Time.unscaledTimeAsDouble / duration) * duration;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 场景加载完成
        /// </summary>
        internal void OnLoadComplete()
        {
            switch (Mode)
            {
                case EntryMode.Host:
                    Server.LoadSceneComplete(sceneName);
                    Client.LoadSceneComplete(SceneManager.GetActiveScene().name);
                    break;
                case EntryMode.Server:
                    Server.LoadSceneComplete(sceneName);
                    break;
                case EntryMode.Client:
                    Client.LoadSceneComplete(SceneManager.GetActiveScene().name);
                    break;
            }
        }

        /// <summary>
        /// 接收 Ping
        /// </summary>
        /// <param name="pingTime"></param>
        internal static void Ping(double pingTime)
        {
            OnPingUpdate?.Invoke(pingTime);
        }

        /// <summary>
        /// TODO：通过反射进行调用
        /// </summary>
        private static void Window()
        {
            if (!Client.isConnected && !Server.isActive)
            {
                if (!Client.isActive)
                {
                    if (GUILayout.Button("Host (Server + Client)", GUILayout.Height(30)))
                    {
                        StartHost();
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Server", GUILayout.Height(30)))
                    {
                        StartServer();
                    }

                    if (GUILayout.Button("Client", GUILayout.Height(30)))
                    {
                        StartClient();
                    }

                    GUILayout.EndHorizontal();
                }
                else
                {
                    var alignment = GUI.skin.box.alignment;
                    GUI.skin.box.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label($"<b>Connecting...</b>", "Box", GUILayout.Height(30));
                    GUI.skin.box.alignment = alignment;

                    if (GUILayout.Button("Stop Client", GUILayout.Height(30)))
                    {
                        StopClient();
                    }
                }
            }
            else
            {
                var alignment = GUI.skin.box.alignment;
                GUI.skin.box.alignment = TextAnchor.MiddleCenter;
                if (Server.isActive || Client.isActive)
                {
                    GUILayout.Label($"<b>{Transport.address} : {Transport.port}</b>", "Box", GUILayout.Height(30));
                }

                GUI.skin.box.alignment = alignment;
            }

            if (Client.isConnected && !Client.isReady)
            {
                if (GUILayout.Button("Ready", GUILayout.Height(30)))
                {
                    Client.Ready();
                }
            }

            if (Server.isActive && Client.isConnected)
            {
                if (GUILayout.Button("Stop Host", GUILayout.Height(30)))
                {
                    StopHost();
                }
            }
            else if (Client.isConnected)
            {
                if (GUILayout.Button("Stop Client", GUILayout.Height(30)))
                {
                    StopClient();
                }
            }
            else if (Server.isActive)
            {
                if (GUILayout.Button("Stop Server", GUILayout.Height(30)))
                {
                    StopServer();
                }
            }
        }
    }
}