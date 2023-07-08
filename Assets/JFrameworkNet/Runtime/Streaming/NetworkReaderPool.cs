using System;
using System.Runtime.CompilerServices;
using JFramework.Core;

namespace JFramework.Net
{
    public static class Reader<T>
    {
        public static Func<NetworkReader, T> read;
    }

    public static class NetworkReaderPool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkReader Pop(byte[] bytes)
        {
            var reader = PoolManager.Pop<NetworkReader>();
            reader.SetBuffer(bytes);
            return reader;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkReader Pop(ArraySegment<byte> segment)
        {
            var reader = PoolManager.Pop<NetworkReader>();
            reader.SetBuffer(segment);
            return reader;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(NetworkReader reader) => PoolManager.Push(reader);
    }
}