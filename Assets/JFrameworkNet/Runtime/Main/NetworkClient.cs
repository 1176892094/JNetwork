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
        private static readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();
        public static ServerObject connection;
        public static bool isReady;
        public static bool isLoadScene;
        private static double lastSendTime;
        private static ConnectState state;
        internal static Action OnConnected;
        internal static Action OnDisconnected;
        private static NetworkReceive receive = new NetworkReceive();
        public static bool isActive => state is ConnectState.Connected or ConnectState.Connecting;
        public static bool isConnect => state == ConnectState.Connected;
        private static uint tickRate => NetworkManager.Instance.tickRate;
        private static float sendRate => tickRate < int.MaxValue ? 1f / tickRate : 0;
        
        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="address">传入连接地址</param>
        internal static void StartClient(Address address)
        {
            RegisterTransport();
            Transport.current.ClientConnect(address);
            ClientConnect(false);
        }
        
        /// <summary>
        /// 开启客户端
        /// </summary>
        /// <param name="uri">传入Uri</param>
        internal static void StartClient(Uri uri)
        {
            RegisterTransport();
            Transport.current.ClientConnect(uri);
            ClientConnect(false);
        }

        /// <summary>
        /// 开启主机，使用Server的Transport
        /// </summary>
        internal static void StartClient()
        {
            ClientConnect(true);
            NetworkServer.client = new ClientObject(NetworkConst.HostId);
            connection.isLocal = true;
        }

        private static void ClientConnect(bool isHost)
        {
            receive = new NetworkReceive();
            state = isHost ? ConnectState.Connected : ConnectState.Connecting;
            connection = new ServerObject();
            RegisterMessage(isHost);
        }

        public static void Ready()
        {
            if (isReady)
            {
                Debug.LogError("Client is already ready !");
                return;
            }

            if (connection == null)
            {
                Debug.LogError("No connection to the Server !");
                return;
            }

            isReady = true;
            connection.isReady = true;
            connection.Send(new ReadyMessage());
        }

        public static void Disconnect()
        {
            if (state is ConnectState.Disconnecting or ConnectState.Disconnected)
            {
                return;
            }

            state = ConnectState.Disconnecting;
            isReady = false;
            connection?.Disconnect();
        }

        public static void Send<T>(T message, Channel channelId = Channel.Reliable) where T : struct, NetworkMessage
        {
            if (connection != null)
            {
                if (state == ConnectState.Connected)
                {
                    connection.Send(message, channelId);
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

        public static void StopClient()
        {
            state = ConnectState.Disconnected;
            spawns.Clear();
            connection = null;
            isReady = false;
            isLoadScene = false;
            OnConnected = null;
            OnDisconnected = null;
        }
    }
}