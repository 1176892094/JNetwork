using System;
using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class Server : IConnection
    {
        private Socket socket;
        private EndPoint endPoint;
        private ConnectionState state;
        private readonly Setting setting;
        private readonly ServerData serverData;

        public Server(Setting setting, ServerData serverData)
        {
            this.setting = setting;
            this.serverData = serverData;
        }

        public void Connect(IConfig config)
        {
            if (socket != null)
            {
                Log.Info("Server is already connected");
                return;
            }

            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                socket.DualMode = true;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to set Dual Mode.\n{e}");
            }

            socket.Bind(new IPEndPoint(IPAddress.IPv6Any, config.port));
            socket.Blocking = false;
            socket.SendBufferSize = setting.sendBufferSize;
            socket.ReceiveBufferSize = setting.receiveBufferSize;
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