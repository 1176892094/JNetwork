namespace Transport
{
    public enum Header : byte
    {
        Handshake = 1,
        Ping = 2,
        Message = 3,
        Disconnect = 4
    }
}