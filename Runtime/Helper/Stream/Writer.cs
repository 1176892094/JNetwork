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
using System.Runtime.CompilerServices;
using JFramework.Core;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace JFramework.Net
{
    [Serializable]
    public partial class NetworkWriter : IDisposable
    {
        /// <summary>
        /// 当前字节数组中的位置
        /// </summary>
        internal int position;

        /// <summary>
        /// 缓存的字节数组
        /// </summary>
        internal byte[] buffer = new byte[1500];

        /// <summary>
        /// 将数据序列化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void Write<T>(T value) where T : unmanaged
        {
            AddCapacity(position + sizeof(T));
            fixed (byte* ptr = &buffer[position])
            {
#if UNITY_ANDROID
                var buffers = stackalloc T[1] { value };
                UnsafeUtility.MemCpy(ptr, buffers, sizeof(T));
#else
                *(T*)ptr = value;
#endif
            }

            position += sizeof(T);
        }

        /// <summary>
        /// 写入可空数据
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WriteEmpty<T>(T? value) where T : unmanaged
        {
            if (!value.HasValue)
            {
                Write((byte)0);
                return;
            }

            Write((byte)1);
            Write(value.Value);
        }

        /// <summary>
        /// 重置位置
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reset()
        {
            position = 0;
        }

        /// <summary>
        /// 将泛型类写入委托
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke<T>(T value)
        {
            Writer<T>.write?.Invoke(this, value);
        }

        /// <summary>
        /// 对象池取出对象
        /// </summary>
        /// <returns>返回NetworkWriter类</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkWriter Pop()
        {
            var writer = PoolManager.Dequeue<NetworkWriter>();
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
            PoolManager.Enqueue(writer);
        }

        /// <summary>
        /// 重写字符串转化方法
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return BitConverter.ToString(buffer, 0, position);
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        void IDisposable.Dispose()
        {
            PoolManager.Enqueue(this);
        }
    }

    public partial class NetworkWriter
    {
        /// <summary>
        /// 确保容量
        /// </summary>
        /// <param name="length">传入长度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddCapacity(int length)
        {
            if (buffer.Length < length)
            {
                Array.Resize(ref buffer, Math.Max(length, buffer.Length * 2));
            }
        }

        /// <summary>
        /// 转化为数组分片
        /// </summary>
        /// <returns>返回数组分片</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ArraySegment<byte>(NetworkWriter writer)
        {
            return new ArraySegment<byte>(writer.buffer, 0, writer.position);
        }

        /// <summary>
        /// 写入Byte数组
        /// </summary>
        public void WriteBytes(byte[] segment, int offset, int count)
        {
            AddCapacity(position + count);
            Buffer.BlockCopy(segment, offset, buffer, position, count);
            position += count;
        }
    }
}