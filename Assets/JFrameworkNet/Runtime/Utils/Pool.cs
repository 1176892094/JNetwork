using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JFramework.Net
{
    internal sealed class Pool<T> where T : new()
    {
        private readonly Stack<T> objects = new Stack<T>();

        public Pool(int capacity)
        {
            for (int i = 0; i < capacity; ++i)
            {
                objects.Push(new T());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop() => objects.Count > 0 ? objects.Pop() : new T();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T item) => objects.Push(item);
    }
}