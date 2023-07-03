namespace JFramework.Net
{
    public sealed class ClientConnection : NetworkConnection
    {
        public int clientId;
        public bool isLocal;
        public bool isReady;
        public bool isAuthority;
    }
}