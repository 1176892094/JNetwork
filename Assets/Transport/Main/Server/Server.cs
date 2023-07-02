namespace Transport
{
    public class Server : IConnection
    {
        private readonly ServerData serverData;

        public Server(ServerData serverData)
        {
            this.serverData = serverData;
        }

        public void Connect(IConfig config)
        {
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