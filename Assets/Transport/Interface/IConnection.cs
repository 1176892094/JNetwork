namespace Transport
{
    public interface IConnection
    {
        void Connect(IConfig config);

        void Disconnect();

        void Send();

        void Receive();
    }
}