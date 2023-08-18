using System;
using System.Net;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    internal class NetworkTransport : Transport
    {
        [SerializeField] private bool noDelay = true;
        [SerializeField] private bool congestion = true;
        [SerializeField] private int resend = 2;
        [SerializeField] private int timeout = 10000;
        [SerializeField] private int maxTransmitUnit = 1200;
        [SerializeField] private int sendBufferSize = 1024 * 1027 * 7;
        [SerializeField] private int receiveBufferSize = 1024 * 1027 * 7;
        [SerializeField] private uint sendPacketSize = 1024 * 4;
        [SerializeField] private uint receivePacketSize = 1024 * 4;
        [SerializeField] private uint interval = 10;
        private Setting setting;
        private Client client;
        private Server server;

        private void Awake()
        {
            Log.Info = Debug.Log;
            Log.Warn = Debug.LogWarning;
            Log.Error = Debug.LogError;
            setting = new Setting(sendBufferSize, receiveBufferSize, maxTransmitUnit, timeout, receivePacketSize, sendPacketSize, interval, resend, noDelay, congestion);
            client = new Client(setting, ClientConnected, ClientDisconnected, ClientDataReceived);
            server = new Server(setting, ServerConnected, ServerDisconnected, ServerDataReceived);

            void ClientConnected()
            {
                OnClientConnected.Invoke();
            }

            void ClientDataReceived(ArraySegment<byte> message, Udp.Channel channel)
            {
                OnClientReceive.Invoke(message, (Channel)channel);
            }

            void ClientDisconnected()
            {
                OnClientDisconnected.Invoke();
            }

            void ServerConnected(int connectionId)
            {
                OnServerConnected.Invoke(connectionId);
            }

            void ServerDataReceived(int connectionId, ArraySegment<byte> message, Udp.Channel channel)
            {
                OnServerReceive.Invoke(connectionId, message, (Channel)channel);
            }

            void ServerDisconnected(int connectionId)
            {
                OnServerDisconnected.Invoke(connectionId);
            }
        }

        public override void ClientConnect(string address, ushort port) => client.Connect(address, port);

        public override void ClientConnect(Uri uri)
        {
            int newPort = uri.IsDefaultPort ? port : uri.Port;
            client.Connect(uri.Host, (ushort)newPort);
        }

        public override void ClientSend(ArraySegment<byte> segment, Channel channel)
        {
            client.Send(segment, (Udp.Channel)channel);
            OnClientSend?.Invoke(segment, channel);
        }

        public override void ClientDisconnect() => client.Disconnect();

        public override void StartServer() => server.Connect(port);

        public override void ServerSend(int clientId, ArraySegment<byte> segment, Channel channel)
        {
            server.Send(clientId, segment, (Udp.Channel)channel);
            OnServerSend?.Invoke(clientId, segment, channel);
        }

        public override void ServerDisconnect(int clientId) => server.Disconnect(clientId);
        public override Uri GetServerUri()
        {
            var builder = new UriBuilder
            {
                Scheme = "https",
                Host = Dns.GetHostName(),
                Port = port
            };
            return builder.Uri;
        }

        public override int GetMaxPacketSize(Channel channel = Channel.Reliable)
        {
            return channel == Channel.Reliable ? Helper.ReliableSize(setting.maxTransferUnit, receivePacketSize) : Helper.UnreliableSize(setting.maxTransferUnit);
        }

        public override int UnreliableSize() => Helper.UnreliableSize(maxTransmitUnit);

        public override void StopServer() => server.StopServer();

        public override void ClientEarlyUpdate()
        {
            if (enabled)
            {
                client.EarlyUpdate();
            }
        }

        public override void ClientAfterUpdate() => client.AfterUpdate();

        public override void ServerEarlyUpdate()
        {
            if (enabled)
            {
                server.EarlyUpdate();
            }
        }

        public override void ServerAfterUpdate() => server.AfterUpdate();
    }
}