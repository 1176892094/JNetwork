using System;
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
            client = new Client(setting, OnClientConnected, OnClientDisconnected, OnClientReceive);
            server = new Server(setting, OnServerConnected, OnServerDisconnected, OnServerReceive);
        }

        public override void ClientConnect(Address address) => client.Connect(address.ip, address.port);

        public override void ClientConnect(Uri uri)
        {
            int port = uri.IsDefaultPort ? address.port : uri.Port;
            client.Connect(uri.Host, (ushort)port);
        }

        public override void ClientSend(ArraySegment<byte> segment, Channel channel)
        {
            client.Send(segment, channel);
            OnClientSend?.Invoke(segment, channel);
        }

        public override void ClientDisconnect() => client.Disconnect();

        public override void StartServer() => server.Connect(address.port);

        public override void ServerSend(int clientId, ArraySegment<byte> segment, Channel channel)
        {
            server.Send(clientId, segment, channel);
            OnServerSend?.Invoke(clientId, segment, channel);
        }

        public override void ServerDisconnect(int clientId) => server.Disconnect(clientId);

        public override int GetMaxPacketSize(Channel channel = Channel.Reliable)
        {
            return channel == Channel.Reliable ? Utils.ReliableSize(setting.maxTransferUnit, receivePacketSize) : Utils.UnreliableSize(setting.maxTransferUnit);
        }

        public override int UnreliableSize() => Utils.UnreliableSize(maxTransmitUnit);

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