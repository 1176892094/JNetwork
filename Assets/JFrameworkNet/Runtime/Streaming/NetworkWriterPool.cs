using System.Runtime.CompilerServices;

namespace JFramework.Net
{
    public static class NetworkWriterPool
    {
        private static readonly Pool<NetworkWriter> Pool = new Pool<NetworkWriter>( 1000);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkWriter Pop()
        {
            var writer = Pool.Pop();
            writer.Reset();
            return writer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(NetworkWriter writer) => Pool.Push(writer);
    }
}