using System.Runtime.CompilerServices;
using JFramework.Udp;

namespace JFramework.Net
{
    public class NetworkConnection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Send<T>(T message, Channel channel = Channel.Reliable) where T : struct, NetworkMessage
        {
            // using var writer = NetworkWriterPool.Pop();
            // MessageUtils.Writer(message, writer);
            // Send(writer.ToArraySegment(), channel);
        }
    }
}