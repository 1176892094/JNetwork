using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable All

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        private static readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();
        private static readonly Dictionary<int, ClientConnection> clients = new Dictionary<int, ClientConnection>();
        private static readonly List<ClientConnection> copies = new List<ClientConnection>();
        private static bool initialized;
        private static double lastSendTime;
        public static bool isActive;
        public static bool isLoadScene;
        public static ClientConnection connection;
        internal static Action<ClientConnection> OnConnected;
        internal static Action<ClientConnection> OnDisconnected;
        private static uint tickRate => NetworkManager.Instance.tickRate;
        private static uint maxConnection => NetworkManager.Instance.maxConnection;
        private static float sendRate => tickRate < int.MaxValue ? 1f / tickRate : 0;

        internal static void StartServer(bool isListen)
        {
            if (isListen)
            {
                Transport.current.ServerConnect();
            }

            if (!initialized)
            {
                initialized = true;
                isActive = true;
                clients.Clear();
                RegisterMessage();
                RegisterTransport();
                NetworkTime.RuntimeInitializeOnLoad();
            }

            SpawnObjects();
        }

        internal static void OnClientConnect(ClientConnection client)
        {
            if (!clients.ContainsKey(client.clientId))
            {
                clients[client.clientId] = client;
            }

            OnConnected?.Invoke(client);
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

        private static void DisconnectClients()
        {
            foreach (var connection in clients.Values.ToList())
            {
                connection.Disconnect();
                if (connection.clientId != NetworkConst.ConnectionId)
                {
                    OnServerDisconnected(connection.clientId);
                }
            }
        }

        public static void StopServer()
        {
            if (initialized)
            {
                initialized = false;
                Transport.current.ServerStop();
                UnRegisterTransport();
            }

            connection = null;
            spawns.Clear();
            clients.Clear();
            isActive = false;
            isLoadScene = false;
            OnConnected = null;
            OnDisconnected = null;
        }
    }
}