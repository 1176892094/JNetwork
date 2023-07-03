using System;
using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkServer
    {
        private static Dictionary<int, ClientConnection> clientDict;
        public static bool isActive;
        private static bool initialized;
        private static int heartTickRate;
        private static int maxConnection;
        internal static Action<ClientConnection> OnConnected;
        internal static Action<ClientConnection> OnDisconnected;

        public static void Connect()
        {
            StartServer();
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

        public static void SpawnObjects()
        {
        }

        public static void EarlyUpdate()
        {
        }

        public static void AfterUpdate()
        {
        }
    }
}