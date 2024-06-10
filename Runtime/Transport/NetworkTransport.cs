using System;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public class NetworkTransport : Transport
    {
        public int unit = 1200;
        public int timeout = 10000;
        public uint send = 1024;
        public uint receive = 1024;
        public uint resend = 2;
        public uint interval = 10;
        private Client client;
        private Server server;

        private void Awake()
        {
            Log.Info = Debug.Log;
            Log.Warn = Debug.LogWarning;
            Log.Error = Debug.LogError;
            var setting = new Setting(unit, timeout, send, receive, resend, interval);
            client = new Client(setting, ClientConnect, ClientDisconnect, ClientReceive);
            server = new Server(setting, ServerConnect, ServerDisconnect, ServerReceive);

            void ClientConnect() => OnClientConnect.Invoke();
            
            void ClientReceive(ArraySegment<byte> message, int channel) => OnClientReceive.Invoke(message, channel);
            
            void ClientDisconnect() => OnClientDisconnect.Invoke();
            
            void ServerConnect(int clientId) => OnServerConnect.Invoke(clientId);
            
            void ServerReceive(int clientId, ArraySegment<byte> message, int channel) => OnServerReceive.Invoke(clientId, message, channel);
            
            void ServerDisconnect(int clientId) => OnServerDisconnect.Invoke(clientId);
        }

        public override int MessageSize(int channel) => channel == Channel.Reliable ? Utility.ReliableSize(unit, receive) : Utility.UnreliableSize(unit);

        public override void StartServer() => server.Connect(port);

        public override void StopServer() => server.StopServer();

        public override void StopClient(int clientId) => server.Disconnect(clientId);

        public override void SendToClient(int clientId, ArraySegment<byte> segment, int channel = Channel.Reliable) => server.Send(clientId, segment, channel);

        public override void StartClient() => client.Connect(address, port);

        public override void StartClient(Uri uri) => client.Connect(uri.Host, (ushort)(uri.IsDefaultPort ? port : uri.Port));

        public override void StopClient() => client.Disconnect();

        public override void SendToServer(ArraySegment<byte> segment, int channel = Channel.Reliable) => client.Send(segment, channel);

        public override void ClientEarlyUpdate() => client.EarlyUpdate();

        public override void ClientAfterUpdate() => client.AfterUpdate();

        public override void ServerEarlyUpdate() => server.EarlyUpdate();

        public override void ServerAfterUpdate() => server.AfterUpdate();
    }
}