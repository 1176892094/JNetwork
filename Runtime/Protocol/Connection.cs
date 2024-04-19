using System.Net;

namespace JFramework.Udp
{
    internal struct Connection
    {
        public Proxy proxy;
        public readonly EndPoint endPoint;

        public Connection(EndPoint endPoint)
        {
            proxy = null;
            this.endPoint = endPoint;
        }
    }
}