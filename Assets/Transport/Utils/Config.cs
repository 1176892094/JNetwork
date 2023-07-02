namespace Transport
{
    public struct Config : IConfig
    {
        public string address { get; set; }
        public ushort port { get; set; }

        public Config(string address, ushort port)
        {
            this.address = address;
            this.port = port;
        }
    }
}