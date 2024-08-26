using System;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public class NetworkTransport : Transport
    {
        public int maxUnit = 1200;
        public uint timeout = 10000;
        public uint interval = 10;
        public uint deadLink = 40;
        public uint fastResend = 2;
        public uint sendWindow = 1024 * 4;
        public uint receiveWindow = 1024 * 4;
        private Client client;
        private Server server;

        private void Awake()
        {
            Log.Info = Debug.Log;
            Log.Warn = Debug.LogWarning;
            Log.Error = Debug.LogError;
            var setting = new Setting(maxUnit, timeout, interval, deadLink, fastResend, sendWindow, receiveWindow);
            client = new Client(setting, ClientConnect, ClientDisconnect, ClientReceive);
            server = new Server(setting, ServerConnect, ServerDisconnect, ServerReceive);

            void ClientConnect() => OnClientConnect.Invoke();

            void ClientReceive(ArraySegment<byte> message, byte channel) => OnClientReceive.Invoke(message, channel);

            void ClientDisconnect() => OnClientDisconnect.Invoke();

            void ServerConnect(int clientId) => OnServerConnect.Invoke(clientId);

            void ServerReceive(int clientId, ArraySegment<byte> message, byte channel) => OnServerReceive.Invoke(clientId, message, channel);

            void ServerDisconnect(int clientId) => OnServerDisconnect.Invoke(clientId);
        }

        public override int MessageSize(byte channel) => channel == Channel.Reliable ? Common.ReliableSize(maxUnit, receiveWindow) : Common.UnreliableSize(maxUnit);

        public override void StartServer() => server.Connect(port);

        public override void StopServer() => server.StopServer();

        public override void StopClient(int clientId) => server.Disconnect(clientId);

        public override void SendToClient(int clientId, ArraySegment<byte> segment, byte channel = Channel.Reliable) => server.Send(clientId, segment, channel);

        public override void StartClient() => client.Connect(address, port);

        public override void StartClient(Uri uri) => client.Connect(uri.Host, (ushort)(uri.IsDefaultPort ? port : uri.Port));

        public override void StopClient() => client.Disconnect();

        public override void SendToServer(ArraySegment<byte> segment, byte channel = Channel.Reliable) => client.Send(segment, channel);

        public override void ClientEarlyUpdate() => client.EarlyUpdate();

        public override void ClientAfterUpdate() => client.AfterUpdate();

        public override void ServerEarlyUpdate() => server.EarlyUpdate();

        public override void ServerAfterUpdate() => server.AfterUpdate();
    }
}