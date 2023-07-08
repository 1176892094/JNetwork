using System;
using System.Collections.Generic;
using System.Linq;
using JFramework.Udp;
using UnityEngine;

// ReSharper disable All

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        private static readonly Dictionary<ushort, MessageDelegate> messages = new Dictionary<ushort, MessageDelegate>();
        private static readonly Dictionary<uint, NetworkObject> spawns = new Dictionary<uint, NetworkObject>();
        private static readonly Dictionary<int, ClientEntity> clients = new Dictionary<int, ClientEntity>();
        private static readonly List<ClientEntity> copies = new List<ClientEntity>();
        private static bool initialized;
        private static double lastSendTime;
        public static bool isActive;
        public static bool isLoadScene;
        public static ClientEntity connection;
        internal static Action<ClientEntity> OnConnected;
        internal static Action<ClientEntity> OnDisconnected;
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
                Debug.Log("NetworkServer --> StartServer");
            }

            SpawnObjects();
        }

        internal static void OnClientConnect(ClientEntity client)
        {
            if (!clients.ContainsKey(client.clientId))
            {
                clients[client.clientId] = client;
            }

            Debug.Log($"NetworkServer --> Connected: {client.clientId}");
            OnConnected?.Invoke(client);
        }

        internal static void SetClientReady(ClientEntity client)
        {
            client.isReady = true;
        }

        private static void SetClientNotReady(ClientEntity client)
        {
            client.isReady = false;
            client.Send(new NotReadyMessage());
        }

        public static void SetClientNotReadyAll()
        {
            foreach (var connection in clients.Values)
            {
                SetClientNotReady(connection);
            }
        }

        public static void Send<T>(T message, Channel channel = Channel.Reliable, bool ignoreReady = false) where T : struct, IEvent
        {
            if (!isActive)
            {
                Debug.LogWarning("NetworkServer is not active");
                return;
            }

            using var writer = NetworkWriter.Pop();
            NetworkUtils.WriteMessage(writer, message);
            var segment = writer.ToArraySegment();
            foreach (var client in clients.Values.Where(client => !ignoreReady || client.isReady))
            {
                client.Send(segment, channel);
            }
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
            messages.Clear();
            isActive = false;
            isLoadScene = false;
            OnConnected = null;
            OnDisconnected = null;
        }
    }
}