using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JFramework.Net
{
    internal sealed class Pool<T>
    {
        private readonly Stack<T> objects = new Stack<T>();
        private readonly Func<T> onPop;

        public Pool(Func<T> onPop, int capacity)
        {
            this.onPop = onPop;
            for (int i = 0; i < capacity; ++i)
            {
                objects.Push(onPop());
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop() => objects.Count > 0 ? objects.Pop() : onPop();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T item) => objects.Push(item);
    }
}