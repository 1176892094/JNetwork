using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable All

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        private static readonly Dictionary<uint, NetworkIdentity> spawns = new Dictionary<uint, NetworkIdentity>();
        private static readonly Dictionary<int, ClientConnection> clients = new Dictionary<int, ClientConnection>();
        private static readonly Dictionary<ushort, MessageDelegate> messages = new Dictionary<ushort, MessageDelegate>();
        private static bool initialized;
        private static int heartTickRate;
        private static int maxConnection;
        public static bool isActive;
        public static bool isListen = true;
        public static bool isLoadScene;
        public static ClientConnection host;
        internal static Action<ClientConnection> OnConnected;
        internal static Action<ClientConnection> OnDisconnected;

        internal static void Connect()
        {
            if (!Transport.Instance)
            {
                Debug.LogError("There was no active Transport!");
                return;
            }

            if (isListen)
            {
                Transport.Instance.ServerStart(); // 创建Socket
            }

            StartServer();
            isActive = true;
            heartTickRate = NetworkManager.Instance.heartTickRate;
            maxConnection = NetworkManager.Instance.maxConnection;
            RegisterMessage();
        }

        private static void StartServer()
        {
            if (initialized) return;
            initialized = true;
            clients.Clear();
            NetworkTime.RuntimeInitializeOnLoad();
            AddTransportEvent();
        }

        internal static void OnConnect(ClientConnection client)
        {
            AddConnection(client);
            OnConnected?.Invoke(client);
        }

        private static void AddConnection(ClientConnection client)
        {
            if (!clients.ContainsKey(client.clientId))
            {
                clients[client.clientId] = client;
            }
        }

        internal static void SetClientReady(ClientConnection client)
        {
            client.isReady = true;
            AddObserversForClient(client);
        }

        private static void AddObserversForClient(ClientConnection client)
        {
            if (!client.isReady) return;
            client.Send(new ObjectSpawnStartMessage());
            foreach (var identity in spawns.Values.Where(identity => identity.gameObject.activeSelf))
            {
                identity.AddObserver(client);
            }

            client.Send(new ObjectSpawnFinishMessage());
        }

        internal static void SpawnObjects()
        {
        }
        
        private static void DisconnectAll()
        {
            foreach (var connection in clients.Values.ToList())
            {
                connection.Disconnect();
                if (connection.clientId != NetworkConst.ConnectionId)
                {
                    OnServerDisconnected(connection.clientId);
                }
            }
            
            host = null;
            isListen = true;
            isLoadScene = false;
            spawns.Clear();
            clients.Clear();
            messages.Clear();
            OnConnected = null;
            OnDisconnected = null;
        }

        public static void RuntimeInitializeOnLoad()
        {
            if (initialized)
            {
                initialized = false;
                Transport.Instance.ServerStop();
                RemoveTransportEvent();
            }
        }

        internal static void EarlyUpdate()
        {
            if (Transport.Instance != null)
            {
                Transport.Instance.ServerEarlyUpdate();
            }
        }

        internal static void AfterUpdate()
        {
            if (Transport.Instance != null)
            {
                Transport.Instance.ServerAfterUpdate();
            }
        }
    }
}