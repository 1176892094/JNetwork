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
        public static Server server;
        public static bool isActive;
        public static bool isReady;
        public static bool isLoadScene;
        private static ConnectState state;
        internal static Action OnConnected;
        internal static Action OnDisconnected;
        private static NetworkReceive unBatch = new NetworkReceive();

        public static void Connect(Address address)
        {
            StartClient(false);
            AddClientEvent();
            state = ConnectState.Connecting;
            Transport.Instance.ClientConnect(address);
            server = new Server();
        }

        public static void Connect(Uri uri)
        {
            StartClient(false);
            AddClientEvent();
            state = ConnectState.Connecting;
            Transport.Instance.ClientConnect(uri);
            server = new Server();
        }

        public static void ConnectHost()
        {
            StartClient(true);
            state = ConnectState.Connected;
            var localServer = new Server();
            var localClient = new Client(0);
            server = localServer;
            NetworkServer.client = localClient;
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
        
        internal static void OnClientReceive(ArraySegment<byte> data, Channel channel)
        {
            if (server != null)
            {
                if (!unBatch.ReadEnqueue(data))
                {
                    Debug.LogWarning($"NetworkClient: failed to add batch, disconnecting.");
                    server.Disconnect();
                    return;
                }
                
                while (!isLoadScene && unBatch.ReadDequeue(out NetworkReader reader, out double remoteTimestamp))
                {
                    if (reader.Remaining >= NetworkConst.IdSize)
                    {
                        // server.timestamp = remoteTimestamp;
                        //
                        // if (!DecodeAndInvoke(reader, channel))
                        // {
                        //     Debug.LogWarning($"NetworkClient: failed to unpack and invoke message. Disconnecting.");
                        //     server.Disconnect();
                        //     return;
                        // }
                    }
                    else
                    {
                        Debug.LogWarning($"NetworkClient: received Message was too short (messages should start with message id)");
                        server.Disconnect();
                        return;
                    }
                }
                
                if (!isLoadScene && unBatch.batchCount > 0)
                {
                    Debug.LogError($"Still had {unBatch.batchCount} batches remaining after processing, even though processing was not interrupted by a scene change.\n");
                }
            }
            else
            {
                Debug.LogError("Skipped Data message handling because server is null.");
            }
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