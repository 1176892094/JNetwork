namespace JFramework.Udp
{
    internal enum Header : byte
    {
        Handshake = 1,
        Ping = 2,
        Message = 3,
        Disconnect = 4
    }

    internal enum State : byte
    {
        Connected = 0,
        Authority = 1,
        Disconnected = 2,
    }

    public enum Channel : byte
    {
        Reliable = 1,
        Unreliable = 2
    }
}