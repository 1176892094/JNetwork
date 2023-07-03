using System.Runtime.CompilerServices;
using JFramework.Udp;

namespace JFramework.Net
{
    public abstract class NetworkConnection
    {
        public readonly int connectionId;
        public bool isAuthority;
        public bool isReady;

        internal NetworkConnection()
        {
        }

        internal NetworkConnection(int connectionId)
        {
            this.connectionId = connectionId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send<T>(T message, Channel channel = Channel.Reliable) where T : struct, NetworkMessage
        {
            // using var writer = NetworkWriterPool.Pop();
            // MessageUtils.Writer(message, writer);
            // Send(writer.ToArraySegment(), channel);
        }
    }
}