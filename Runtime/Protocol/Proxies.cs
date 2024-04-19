using System.Net;

namespace JFramework.Udp
{
    internal struct Proxies
    {
        public Proxy proxy;
        public readonly EndPoint endPoint;

        public Proxies(EndPoint endPoint)
        {
            proxy = null;
            this.endPoint = endPoint;
        }
    }
}