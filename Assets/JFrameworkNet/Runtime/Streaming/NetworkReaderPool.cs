using System;
using System.Runtime.CompilerServices;

namespace JFramework.Net
{
    public static class Reader<T>
    {
        public static Func<NetworkReader, T> read;
    }

    public static class NetworkReaderPool
    {
        private static readonly Pool<NetworkReader> Pool = new Pool<NetworkReader>(1000);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkReader Pop(byte[] bytes)
        {
            var reader = Pool.Pop();
            reader.SetBuffer(bytes);
            return reader;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkReader Pop(ArraySegment<byte> segment)
        {
            var reader = Pool.Pop();
            reader.SetBuffer(segment);
            return reader;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(NetworkReader reader) => Pool.Push(reader);
    }
}