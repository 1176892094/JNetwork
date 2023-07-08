using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable All

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        private static readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();
        private static readonly Dictionary<int, ClientObject> clients = new Dictionary<int, ClientObject>();
        private static readonly List<ClientObject> copies = new List<ClientObject>();
        private static bool initialized;
        private static double lastSendTime;
        public static bool isActive;
        public static bool isLoadScene;
        public static ClientObject connection;
        internal static Action<ClientObject> OnConnected;
        internal static Action<ClientObject> OnDisconnected;
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

        internal static void OnClientConnect(ClientObject client)
        {
            if (!clients.ContainsKey(client.clientId))
            {
                clients[client.clientId] = client;
            }

            OnConnected?.Invoke(client);
        }

        internal static void SetClientReady(ClientObject client)
        {
            client.isReady = true;
        }
        
        internal static void SpawnObjects()
        {
        }

        private static void DisconnectClients()
        {
            foreach (var connection in clients.Values.ToList())
            {
                connection.Disconnect();
                if (connection.clientId != NetworkConst.HostId)
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