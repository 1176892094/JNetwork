namespace JFramework.Net
{
    public enum RpcType : byte
    {
        ServerRpc,
        ClientRpc
    }

    internal enum NetworkMode : byte
    {
        None = 0,
        Client = 1,
        Server = 2,
        Host = 3
    }

    internal enum ConnectState : byte
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Disconnecting = 3,
    }

    internal enum SyncMode : byte
    {
        Owner,
        Observer,
    }

    internal enum SyncDirection : byte
    {
        ServerToClient,
        ClientToServer
    }

    public enum Channel : byte
    {
        Reliable = 1,
        Unreliable = 2
    }
}