using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class Utils
    {
        public static bool TryGetAddress(string host, out IPAddress address)
        {
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(host);
                address = addresses[0];
                return addresses.Length >= 1;
            }
            catch (SocketException exception)
            {
                Log.Info($"Failed to resolve host: {host}\n{exception}");
                address = null;
                return false;
            }
        }
    }
}