namespace JFramework.Net
{
    public sealed class ClientConnection : NetworkConnection
    {
        public bool isLocal;

        public ClientConnection(int connectionId) : base(connectionId)
        {
        }
    }
}