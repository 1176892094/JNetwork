using System.Net;

namespace Transport
{
    internal class Connection
    {
        public Peer peer;
        public readonly EndPoint endPoint;

        public Connection(EndPoint endPoint)
        {
            this.endPoint = endPoint;
        }
    }
}