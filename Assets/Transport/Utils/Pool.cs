using System;
using System.Collections.Generic;

namespace Transport
{
    internal class Pool<T>
    {
        private readonly Stack<T> objects = new Stack<T>();
        private readonly Func<T> poolEvent;
        private readonly Action<T> resetEvent;

        public Pool(Func<T> poolEvent, Action<T> resetEvent, int initialCapacity)
        {
            this.poolEvent = poolEvent;
            this.resetEvent = resetEvent;
            for (var i = 0; i < initialCapacity; ++i)
            {
                objects.Push(poolEvent());
            }
        }

        public T Pop() => objects.Count > 0 ? objects.Pop() : poolEvent();

        public void Push(T item)
        {
            resetEvent(item);
            objects.Push(item);
        }
    }
}