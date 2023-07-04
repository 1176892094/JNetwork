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
        private static readonly Dictionary<ushort, MessageDelegate> messages = new Dictionary<ushort, MessageDelegate>();
        public static ServerConnection server;
        public static bool isReady;
        public static bool isLoadScene;
        private static ConnectState state;
        internal static Action OnConnected;
        internal static Action OnDisconnected;
        private static NetworkReceive receive = new NetworkReceive();
        public static bool isActive => state is ConnectState.Connected or ConnectState.Connecting;
        public static bool connected => state == ConnectState.Connected;

        public static void Connect(Address address)
        {
            StartClient(false);
            AddClientEvent();
            state = ConnectState.Connecting;
            Transport.Instance.ClientConnect(address);
            server = new ServerConnection();
        }

        public static void Connect(Uri uri)
        {
            StartClient(false);
            AddClientEvent();
            state = ConnectState.Connecting;
            Transport.Instance.ClientConnect(uri);
            server = new ServerConnection();
        }

        public static void ConnectHost()
        {
            StartClient(true);
            state = ConnectState.Connected;
            server = new ServerConnection();
            NetworkServer.host = new ClientConnection(0);
        }

        private static void StartClient(bool hostMode)
        {
            if (Transport.Instance == null)
            {
                Debug.LogError("There was no active Transport!");
                return;
            }

            //TODO: RegisterMessageHandlers(hostMode);
            Transport.Instance.enabled = true;
        }

        private static void AddClientEvent()
        {
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
            messages.Clear();
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