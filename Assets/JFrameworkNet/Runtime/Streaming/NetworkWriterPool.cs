using System;
using System.Runtime.CompilerServices;

namespace JFramework.Net
{
    public static class NetworkWriterPool
    {
        private static readonly Pool<NetworkWriterObject> Pool = new Pool<NetworkWriterObject>(() => new NetworkWriterObject(), 1000);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkWriterObject Pop()
        {
            var writer = Pool.Pop();
            writer.Reset();
            return writer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(NetworkWriterObject writer) => Pool.Push(writer);
    }

    public class NetworkWriterObject : NetworkWriter, IDisposable
    {
        public void Dispose() => NetworkWriterPool.Push(this);
    }
}