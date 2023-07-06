using System;
using System.Collections.Generic;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public enum ConnectState : byte
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Disconnecting = 3,
    }

    public static partial class NetworkClient
    {
        private static readonly Dictionary<uint, NetworkIdentity> spawns = new Dictionary<uint, NetworkIdentity>();
        public static ServerConnection server;
        public static bool isReady;
        public static bool isLoadScene;
        private static ConnectState state;
        internal static Action OnConnected;
        internal static Action OnDisconnected;
        private static NetworkReceive receive = new NetworkReceive();
        public static bool isActive => state is ConnectState.Connected or ConnectState.Connecting;
        public static bool connected => state == ConnectState.Connected;

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="address">传入地址</param>
        public static void StartClient(Address address)
        {
            if (!TryConnect(false)) return;
            RegisterTransport();
            state = ConnectState.Connecting;
            Transport.Instance.ClientConnect(address);
        }

        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="uri">传入Uri</param>
        public static void StartClient(Uri uri)
        {
            if (!TryConnect(false)) return;
            RegisterTransport();
            state = ConnectState.Connecting;
            Transport.Instance.ClientConnect(uri);
        }

        /// <summary>
        /// 开启主机，无需注册传输(使用服务器)
        /// </summary>
        public static void StartHostClient()
        {
            if (!TryConnect(true)) return;
            state = ConnectState.Connected;
            NetworkServer.host = new ClientConnection(0);
        }

        /// <summary>
        /// 尝试连接并注册事件
        /// </summary>
        private static bool TryConnect(bool isHost)
        {
            if (Transport.Instance == null)
            {
                Debug.LogError("There was no active Transport!");
                return false;
            }

            server = new ServerConnection();
            RegisterMessage(isHost);
            return true;
        }

        public static void Ready()
        {
        }

        public static void Disconnect()
        {
            if (state is ConnectState.Disconnecting or ConnectState.Disconnected)
            {
                return;
            }

            state = ConnectState.Disconnecting;
            isReady = false;
            server?.Disconnect();
        }

        public static void Send<T>(T message, Channel channelId = Channel.Reliable) where T : struct, NetworkMessage
        {
            if (server != null)
            {
                if (state == ConnectState.Connected)
                {
                    server.Send(message, channelId);
                }
                else
                {
                    Debug.LogError("NetworkClient Send when not connected to a server");
                }
            }
            else
            {
                Debug.LogError("NetworkClient Send with no connection");
            }
        }

        public static void RuntimeInitializeOnLoad()
        {
            state = ConnectState.Disconnected;
            spawns.Clear();
            server = null;
            isReady = false;
            isLoadScene = false;
            OnConnected = null;
            OnDisconnected = null;
        }

        public static void EarlyUpdate()
        {
            if (Transport.Instance != null)
            {
                Transport.Instance.ClientEarlyUpdate();
            }
        }

        public static void AfterUpdate()
        {
            if (Transport.Instance != null)
            {
                Transport.Instance.ClientAfterUpdate();
            }
        }
    }
}