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
        private static bool initialized;
        public static bool isActive;
        public static bool isLoadScene;
        public static ClientConnection host;
        internal static Action<ClientConnection> OnConnected;
        internal static Action<ClientConnection> OnDisconnected;
        private static int heartTickRate => NetworkManager.Instance.heartTickRate;
        private static int maxConnection => NetworkManager.Instance.maxConnection;

        internal static void StartServer(bool isListen)
        {
            if (!Transport.Instance)
            {
                Debug.LogError("There was no active Transport!");
                return;
            }

            if (isListen)
            {
                Transport.Instance.ServerConnect();
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

            host = null;
            isLoadScene = false;
            spawns.Clear();
            clients.Clear();
            OnConnected = null;
            OnDisconnected = null;
        }

        public static void RuntimeInitializeOnLoad()
        {
            if (initialized)
            {
                initialized = false;
                Transport.Instance.ServerStop();
                UnRegisterTransport();
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