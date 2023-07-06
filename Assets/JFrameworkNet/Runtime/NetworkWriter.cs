using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace JFramework.Net
{
    public class NetworkWriter
    {
        /// <summary>
        /// 字符串编码
        /// </summary>
        internal readonly UTF8Encoding encoding = new UTF8Encoding(false, true);
        
        /// <summary>
        /// 当前字节数组中的位置
        /// </summary>
        internal int position;

        /// <summary>
        /// 缓存的字节数组
        /// </summary>
        internal byte[] buffer = new byte[1500];
        
        /// <summary>
        /// 重置位置
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => position = 0;

        /// <summary>
        /// 确保容量
        /// </summary>
        /// <param name="value">传入长度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EnsureCapacity(int value)
        {
            if (buffer.Length >= value) return;
            int capacity = Math.Max(value, buffer.Length * 2);
            Array.Resize(ref buffer, capacity);
        }

        /// <summary>
        /// 转化为数组
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToArray()
        {
            byte[] data = new byte[position];
            Array.ConstrainedCopy(buffer, 0, data, 0, position);
            return data;
        }
        
        /// <summary>
        /// 转化为数组分片
        /// </summary>
        /// <returns>返回数组分片</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySegment<byte> ToArraySegment() => new ArraySegment<byte>(buffer, 0, position);

        /// <summary>
        /// 将writer直接转化成数组分片
        /// </summary>
        /// <param name="writer">将数组分片隐式转换成NetworkWriter</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ArraySegment<byte>(NetworkWriter writer) => writer.ToArraySegment();
        
        /// <summary>
        /// 将Blittable的数据进行内存拷贝
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void WriteBlittable<T>(T value) where T : unmanaged
        {
#if UNITY_EDITOR
            if (!UnsafeUtility.IsBlittable(typeof(T)))
            {
                Debug.LogError($"{typeof(T)} is not blittable!");
                return;
            }
#endif
            int size = sizeof(T);
            EnsureCapacity(position + size);
            
            fixed (byte* ptr = &buffer[position])
            {
#if UNITY_ANDROID
                T* valueBuffer = stackalloc T[1]{value};
                UnsafeUtility.MemCpy(ptr, valueBuffer, size);
#else
                *(T*)ptr = value;
#endif
            }
            position += size;
        }

        /// <summary>
        /// 写入Byte数组
        /// </summary>
        public void WriteBytes(byte[] array, int offset, int count)
        {
            EnsureCapacity(position + count);
            Array.ConstrainedCopy(array, offset, buffer, position, count);
            position += count;
        }

        /// <summary>
        /// 将泛型类写入委托
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T value)
        {
            Debug.Log(typeof(T));
            Action<NetworkWriter, T> writeDelegate = Writer<T>.write;
            if (writeDelegate == null)
            {
                Debug.LogError($"No writer found for {typeof(T)}");
            }
            else
            {
                writeDelegate(this, value);
            }
        }
    }
    
    public static class Writer<T>
    {
        public static Action<NetworkWriter, T> write;
    }
}
