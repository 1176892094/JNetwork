namespace Transport
{
    public interface IConfig
    {
        public string address { get; set; }
        public ushort port { get; set; }
    }
}