namespace Transport
{
    public struct Config
    {
        public string ip;
        public ushort port;

        public Config(string ip, ushort port)
        {
            this.ip = ip;
            this.port = port;
        }
    }
}