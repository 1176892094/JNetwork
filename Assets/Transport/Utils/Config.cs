namespace Transport
{
    public struct Config
    {
        public readonly string address;
        public readonly ushort port;

        public Config(string address, ushort port)
        {
            this.address = address;
            this.port = port;
        }
    }
}