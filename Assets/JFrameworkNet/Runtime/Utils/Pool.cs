using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JFramework.Net
{
    public class Pool<T>
    {
        private readonly Stack<T> objects = new Stack<T>();
        private readonly Func<T> poolEvent;

        public Pool(Func<T> poolEvent, int initialCapacity)
        {
            this.poolEvent = poolEvent;
            for (int i = 0; i < initialCapacity; ++i)
            {
                objects.Push(poolEvent());
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop() => objects.Count > 0 ? objects.Pop() : poolEvent();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T item) => objects.Push(item);
    }
}