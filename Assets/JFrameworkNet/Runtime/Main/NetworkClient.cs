using System;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public enum ConnectState
    {
        None,
        Connecting,
        Connected,
        Disconnecting,
        Disconnected
    }
    
    public static class NetworkClient
    {
        public static ServerConnection connection;
        public static bool isActive;
        public static bool isReady;
        private static ConnectState state;
        internal static Action OnConnected;
        internal static Action OnDisconnected;

        public static void Connect(Address address)
        {
            StartClient(false);
            AddClientEvent();
            state = ConnectState.Connecting;
            Transport.Instance.ClientConnect(address);
            connection = new ServerConnection();
        }

        public static void Connect(Uri uri)
        {
            StartClient(false);
            AddClientEvent();
            state = ConnectState.Connecting;
            Transport.Instance.ClientConnect(uri);
            connection = new ServerConnection();
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

        public static void Reset()
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