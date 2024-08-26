// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  02:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Text;
using JFramework.Core;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace JFramework.Net
{
    public static class Reader<T>
    {
        public static Func<NetworkReader, T> read;
    }

    [Serializable]
    public partial class NetworkReader : IDisposable
    {
        /// <summary>
        /// 文本编码
        /// </summary>
        internal readonly UTF8Encoding encoding = new UTF8Encoding(false, true);

        /// <summary>
        /// 当前字节数组中的位置
        /// </summary>
        internal int position;

        /// <summary>
        /// 缓存的字节数组
        /// </summary>
        internal ArraySegment<byte> buffer = new ArraySegment<byte>();

        /// <summary>
        /// 数据反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe T Read<T>() where T : unmanaged
        {
            T value;
            fixed (byte* ptr = &buffer.Array[buffer.Offset + position])
            {
#if UNITY_ANDROID
                var buffers = stackalloc T[1];
                UnsafeUtility.MemCpy(buffers, ptr, sizeof(T));
                value = buffers[0];
#else
                value = *(T*)ptr;
#endif
            }

            position += sizeof(T);
            return value;
        }

        /// <summary>
        /// 读取可空数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T? ReadEmpty<T>() where T : unmanaged
        {
            return Read<byte>() != 0 ? Read<T>() : default(T?);
        }

        /// <summary>
        /// 设置缓存数组
        /// </summary>
        /// <param name="segment"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(ArraySegment<byte> segment)
        {
            buffer = segment;
            position = 0;
        }

        /// <summary>
        /// 读取自动生成的委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>()
        {
            return Reader<T>.read == null ? default : Reader<T>.read.Invoke(this);
        }

        /// <summary>
        /// 对象池取出对象
        /// </summary>
        /// <param name="segment">传入byte数组</param>
        /// <returns>返回一个NetworkReader</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkReader Pop(ArraySegment<byte> segment)
        {
            var reader = PoolManager.Dequeue<NetworkReader>();
            reader.Reset(segment);
            return reader;
        }

        /// <summary>
        /// 对象池推入对象
        /// </summary>
        /// <param name="reader">传入NetworkReader</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(NetworkReader reader)
        {
            PoolManager.Enqueue(reader);
        }

        /// <summary>
        /// 重写字符串转化方法
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count);
        }

        /// <summary>
        /// 使用using来释放
        /// </summary>
        void IDisposable.Dispose()
        {
            PoolManager.Enqueue(this);
        }
    }

    public partial class NetworkReader
    {
        /// <summary>
        /// 剩余长度
        /// </summary>
        public int residue => buffer.Count - position;

        /// <summary>
        /// 内部的读取ArraySegment数组方法
        /// </summary>
        /// <param name="count"></param>
        /// <returns>返回ArraySegment</returns>
        public ArraySegment<byte> ReadArraySegment(int count)
        {
            if (residue < count)
            {
                throw new OverflowException("读取器剩余容量不够!");
            }

            var segment = new ArraySegment<byte>(buffer.Array, buffer.Offset + position, count);
            position += count;
            return segment;
        }
    }
}