using System;
using System.Runtime.CompilerServices;
using JFramework.Core;

namespace JFramework.Net
{
    public static class Writer<T>
    {
        public static Action<NetworkWriter, T> write;
    }

    public static class NetworkWriterPool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkWriter Pop()
        {
            var writer = PoolManager.Pop<NetworkWriter>();
            writer.Reset();
            return writer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(NetworkWriter writer) => PoolManager.Push(writer);
    }
}