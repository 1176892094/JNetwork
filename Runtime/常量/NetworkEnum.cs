namespace JFramework.Net
{
    /// <summary>
    /// Rpc类型
    /// </summary>
    internal enum RpcType : byte
    {
        ServerRpc,
        ClientRpc,
    }

    /// <summary>
    /// 网络模式
    /// </summary>
    internal enum NetworkMode : byte
    {
        None = 0,
        Client = 1,
        Server = 2,
        Host = 3
    }

    /// <summary>
    /// 连接状态
    /// </summary>
    internal enum ConnectState : byte
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
    }

    /// <summary>
    /// 同步模式
    /// </summary>
    internal enum SyncMode : byte
    {
        ServerToClient,
        ClientToServer
    }

    /// <summary>
    /// 传输通道
    /// </summary>
    public enum Channel : sbyte
    {
        Reliable = 1,
        Unreliable = 2
    }
}