namespace JDP
{
    public struct Address
    {
        public readonly string ip;
        public readonly ushort port;

        public Address(string ip, ushort port)
        {
            this.ip = ip;
            this.port = port;
        }
    }
}