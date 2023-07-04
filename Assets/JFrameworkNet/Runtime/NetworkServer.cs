using System;
using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkServer
    {
        private static Dictionary<int, Client> clientDict;
        private static bool initialized;
        private static int heartTickRate;
        private static int maxConnection;
        internal static Action<Client> OnConnected;
        internal static Action<Client> OnDisconnected;
        public static bool isActive;
        public static bool isListen = true;
        public static Client client;

        internal static void Connect()
        {
            StartServer();
            if (isListen) // 设置为false进行单人模式
            {
                Transport.Instance.ServerStart();
            }

            isActive = true;
            maxConnection = NetworkManager.Instance.maxConnection;
            heartTickRate = NetworkManager.Instance.hearTickRate;
        }

        private static void StartServer()
        {
            if (!Transport.Instance)
            {
                Debug.LogError("There was no active Transport!");
                return;
            }

            if (initialized) return;
            initialized = true;
            clientDict.Clear();
            NetworkTime.RuntimeInitializeOnLoad();
            //TODO: AddTransportHandlers
        }

        internal static void OnConnect(Client client)
        {
            AddConnection(client);
            OnConnected?.Invoke(client);
        }

        private static void AddConnection(Client client)
        {
            if (!clientDict.ContainsKey(client.clientId))
            {
                clientDict[client.clientId] = client;
            }
        }

        internal static void SpawnObjects()
        {
        }

        internal static void EarlyUpdate()
        {
        }

        internal static void AfterUpdate()
        {
        }
    }
}