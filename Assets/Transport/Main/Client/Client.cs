using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class Client : IConnection
    {
        private Socket socket;
        private EndPoint endPoint;
        private ConnectionState state;
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
                clientData.onDisconnected();
                return;
            }

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
    }
}