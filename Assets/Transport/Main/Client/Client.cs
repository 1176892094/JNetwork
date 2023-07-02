using System;
using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class Client : IConnection
    {
        private ConnectionState state;

        private Peer peer;
        private Socket socket;
        private EndPoint endPoint;
        private readonly Setting setting;
        private readonly ClientData clientData;

        public Client(Setting setting, ClientData clientData)
        {
            this.setting = setting;
            this.clientData = clientData;
        }

        public void Connect(IConfig config)
        {
            if (state == ConnectionState.Connected)
            {
                Log.Info("Client is already connected");
                return;
            }

            if (!Utils.TryGetAddress(config.address, out var address))
            {
                clientData.onDisconnected?.Invoke();
                return;
            }

            GeneratePeer();
            endPoint = new IPEndPoint(address, config.port);
            socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Blocking = false;
            socket.SendBufferSize = setting.sendBufferSize;
            socket.ReceiveBufferSize = setting.receiveBufferSize;
            socket.Connect(endPoint);
        }

        public void Disconnect()
        {
        }

        public void Send()
        {
        }

        public void Receive()
        {
        }

        private void GeneratePeer()
        {
            var peerData = new PeerData(OnAuthority, OnDisconnected, OnSend, clientData.onReceive);
            peer = new Peer(peerData, setting, 0);

            void OnAuthority()
            {
                Log.Info("Client connected.");
                state = ConnectionState.Connected;
                clientData.onConnected?.Invoke();
            }

            void OnDisconnected()
            {
                Log.Info($"Client disconnected");
                socket.Close();
                peer = null;
                socket = null;
                endPoint = null;
                state = ConnectionState.Disconnected;
                clientData.onDisconnected?.Invoke();
            }

            void OnSend(ArraySegment<byte> segment)
            {
                try
                {
                    socket.SendNonBlocking(segment);
                }
                catch (Exception e)
                {
                    Log.Error($"Client send failed!\n{e}");
                }
            }
        }
    }
}