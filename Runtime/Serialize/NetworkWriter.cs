using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace JFramework.Net
{
    public static class Writer<T>
    {
        public static Action<NetworkWriter, T> write;
    }
    
    [Serializable]
    public class NetworkWriter: IDisposable
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
        [SerializeField] internal byte[] buffer = new byte[1500];
        
        /// <summary>
        /// 将Blittable的数据进行内存拷贝
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void Serialize<T>(T value) where T : unmanaged
        {
            EnsureCapacity(position + sizeof(T));
            fixed (byte* ptr = &buffer[position])
            {
#if UNITY_ANDROID
                T* valueBuffer = stackalloc T[1] { value };
                UnsafeUtility.MemCpy(ptr, valueBuffer, sizeof(T));
#else
                *(T*)ptr = value;
#endif
            }
            position += sizeof(T);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SerializeNone<T>(T? value) where T : unmanaged
        {
            Serialize((byte)(value.HasValue ? 0x01 : 0x00));
            if (value.HasValue)
            {
                Serialize(value.Value);
            }
        }
        
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
        /// 写入Byte数组
        /// </summary>
        public void WriteBytesInternal(byte[] array, int offset, int count)
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
            var writer = Writer<T>.write;
            if (writer == null)
            {
                Debug.LogError($"无法获取写入器。写入器类型：{typeof(T)}");
            }
            else
            {
                writer(this, value);
            }
        }
        
        /// <summary>
        /// 重置位置
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            position = 0;
        }

        /// <summary>
        /// 对象池取出对象
        /// </summary>
        /// <returns>返回NetworkWriter类</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkWriter Pop()
        {
            var writer = StreamPool.Pop<NetworkWriter>();
            writer.Reset();
            return writer;
        }

        /// <summary>
        /// 对象池推入对象
        /// </summary>
        /// <param name="writer">传入NetworkWriter类</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(NetworkWriter writer)
        {
            StreamPool.Push(writer);
        }

        /// <summary>
        /// 重写字符串转化方法
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var segment = ToArraySegment();
            return BitConverter.ToString(segment.Array, segment.Offset, segment.Count);
        }
        
        /// <summary>
        /// 使用using来释放
        /// </summary>
        public void Dispose()
        {
            Push(this);
        }
    }
}
