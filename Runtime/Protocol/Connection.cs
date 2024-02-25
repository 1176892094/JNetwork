using System.Net;

namespace JFramework.Udp
{
    internal struct Connection
    {
        public Peer peer;
        public readonly EndPoint endPoint;

        public Connection(EndPoint endPoint)
        {
            peer = null;
            this.endPoint = endPoint;
        }
    }
}