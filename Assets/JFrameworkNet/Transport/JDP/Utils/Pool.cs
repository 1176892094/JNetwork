using System;
using System.Collections.Generic;

namespace JFramework.Udp
{
    internal sealed class Pool<T>
    {
        private readonly Stack<T> objects = new Stack<T>();
        private readonly Func<T> onPop;
        private readonly Action<T> onPush;

        public Pool(Func<T> onPop, Action<T> onPush, int capacity)
        {
            this.onPop = onPop;
            this.onPush = onPush;
            for (var i = 0; i < capacity; ++i)
            {
                objects.Push(onPop());
            }
        }

        public T Pop() => objects.Count > 0 ? objects.Pop() : onPop();

        public void Push(T item)
        {
            onPush?.Invoke(item);
            objects.Push(item);
        }
    }
}