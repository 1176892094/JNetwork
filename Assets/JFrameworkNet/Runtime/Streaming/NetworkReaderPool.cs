using System;
using System.Runtime.CompilerServices;

namespace JFramework.Net
{
    public static class NetworkReaderPool
    {
        private static readonly Pool<NetworkReaderObject> Pool = new Pool<NetworkReaderObject>(() => new NetworkReaderObject(new byte[]{}), 1000);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkReaderObject Pop(byte[] bytes)
        {
            var reader = Pool.Pop();
            reader.SetBuffer(bytes);
            return reader;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkReaderObject Pop(ArraySegment<byte> segment)
        {
            var reader = Pool.Pop();
            reader.SetBuffer(segment);
            return reader;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(NetworkReaderObject reader) => Pool.Push(reader);
    }
    
    public class NetworkReaderObject : NetworkReader, IDisposable
    {
        internal NetworkReaderObject(byte[] bytes) : base(bytes)
        {
        }
        
        public void Dispose() => NetworkReaderPool.Push(this);
    }
}