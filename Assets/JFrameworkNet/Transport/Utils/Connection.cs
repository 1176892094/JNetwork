using System.Net;

namespace JDP
{
    internal sealed class Connection
    {
        public Peer peer;
        public readonly EndPoint endPoint;

        public Connection(EndPoint endPoint)
        {
            this.endPoint = endPoint;
        }
    }
}