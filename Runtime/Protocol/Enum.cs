namespace JFramework.Udp
{
    public enum State : byte
    {
        Connect,
        Connected,
        Disconnect
    }

    public enum ReliableHeader : byte
    {
        Connect = 1,
        Ping = 2,
        Data = 3,
    }

    public enum UnreliableHeader : byte
    {
        Data = 4,
        Disconnect = 5,
    }
}