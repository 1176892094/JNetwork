// *********************************************************************************
// # Project: Forest
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-09-07  22:09
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System.Collections.Generic;

namespace JFramework.Net
{
    public static class NetworkPool<T> where T : new()
    {
        private static readonly Queue<T> objects = new Queue<T>();
        private static readonly HashSet<T> unique = new HashSet<T>();

        public static T Pop()
        {
            if (objects.Count > 0)
            {
                var obj = objects.Dequeue();
                unique.Remove(obj);
                return obj;
            }

            return new T();
        }

        public static void Push(T obj)
        {
            if (unique.Add(obj))
            {
                objects.Enqueue(obj);
            }
        }
    }
}