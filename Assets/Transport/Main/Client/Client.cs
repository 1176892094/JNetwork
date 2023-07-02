namespace Transport
{
    public class Client : IConnection
    {
        private readonly ClientData clientData;

        public Client(ClientData clientData)
        {
            this.clientData = clientData;
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