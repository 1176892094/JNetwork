namespace JFramework.Udp
{
    internal enum Head : byte
    {
        Connect = 1,
        Ping = 2,
        Data = 3,
        Disconnect = 4
    }

    internal enum State : byte
    {
        Connect = 0,
        Connected = 1,
        Disconnect = 2,
    }
}