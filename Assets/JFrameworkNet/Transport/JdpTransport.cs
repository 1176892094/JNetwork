using System;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public class JdpTransport : MonoBehaviour, Transport
    {
        public static JdpTransport Instance;
        public Action OnClientConnected;
        public Action OnClientDisconnected;
        private Action<ArraySegment<byte>, Channel> OnClientSend;
        public Action<ArraySegment<byte>, Channel> OnClientReceive;
        public Action<int> OnServerConnected;
        public Action<int> OnServerDisconnected;
        private Action<int, ArraySegment<byte>, Channel> OnServerSend;
        public Action<int, ArraySegment<byte>, Channel> OnServerReceive;
        [SerializeField] private bool noDelay = true;
        [SerializeField] private bool congestion = true;
        [SerializeField] private int resend = 2;
        [SerializeField] private uint interval = 10;
        [SerializeField] private int maxTransmitUnit = 1200;
        [SerializeField] private int timeout = 10000;
        [SerializeField] private int sendBufferSize = 1025 * 1027 * 7;
        [SerializeField] private int receiveBufferSize = 1024 * 1027 * 7;
        [SerializeField] private uint sendPacketSize = 1024 * 4;
        [SerializeField] private uint receivePacketSize = 1024 * 4;
        [SerializeField] private Address address;
        private Setting setting;
        private Client client;
        private Server server;
        Address Transport.Address => address;

        private void Awake()
        {
            Log.Info = Debug.Log;
            Log.Warn = Debug.LogWarning;
            Log.Error = Debug.LogError;
            setting = new Setting(sendBufferSize, receiveBufferSize, maxTransmitUnit, timeout, receivePacketSize, sendPacketSize, interval, resend, noDelay, congestion);
            client = new Client(setting, new ClientData(ClientConnected, ClientDisconnected, ClientDataReceived));
            server = new Server(setting, new ServerData(ServerConnected, ServerDisconnected, ServerDataReceived));

            void ClientConnected()
            {
                OnClientConnected.Invoke();
            }

            void ClientDataReceived(ArraySegment<byte> message, Channel channel)
            {
                OnClientReceive.Invoke(message, channel);
            }

            void ClientDisconnected()
            {
                OnClientDisconnected.Invoke();
            }

            void ServerConnected(int connectionId)
            {
                OnServerConnected.Invoke(connectionId);
            }

            void ServerDataReceived(int connectionId, ArraySegment<byte> message, Channel channel)
            {
                OnServerReceive.Invoke(connectionId, message, channel);
            }

            void ServerDisconnected(int connectionId)
            {
                OnServerDisconnected.Invoke(connectionId);
            }
        }

        public void ClientConnect(Address address) => client.Connect(address);

        public void ClientConnect(Uri uri)
        {
            int port = uri.IsDefaultPort ? address.port : uri.Port;
            client.Connect(new Address(uri.Host, (ushort)port));
        }

        public void ClientSend(ArraySegment<byte> segment, Channel channel)
        {
            client.Send(segment, channel);
            OnClientSend?.Invoke(segment, channel);
        }

        public void ClientDisconnect() => client.Disconnect();

        public void ServerStart() => server.Connect(address);

        public void ServerSend(int clientId, ArraySegment<byte> segment, Channel channel)
        {
            server.Send(clientId, segment, channel);
            OnServerSend?.Invoke(clientId, segment, channel);
        }

        public void ServerDisconnect(int clientId) => server.Disconnect(clientId);

        public void ServerStop() => server.ShutDown();

        public void ClientEarlyUpdate()
        {
            if (enabled)
            {
                client.EarlyUpdate();
            }
        }

        public void ClientAfterUpdate() => client.AfterUpdate();

        public void ServerEarlyUpdate()
        {
            if (enabled)
            {
                server.EarlyUpdate();
            }
        }

        public void ServerAfterUpdate() => server.AfterUpdate();
    }
}