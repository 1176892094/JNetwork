using System;
using System.Runtime.CompilerServices;
using System.Text;
using JFramework.Core;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace JFramework.Net
{
    public static class Reader<T>
    {
        public static Func<NetworkReader, T> read;
    }
    
    [Serializable]
    public class NetworkReader : IDisposable
    {
        /// <summary>
        /// 字符串编码
        /// </summary>
        internal readonly UTF8Encoding encoding = new UTF8Encoding(false, true);

        /// <summary>
        /// 当前字节数组中的位置
        /// </summary>
        public int position;

        /// <summary>
        /// 缓存的字节数组
        /// </summary>
        [SerializeField] internal ArraySegment<byte> buffer = new ArraySegment<byte>();
        
        /// <summary>
        /// 剩余长度
        /// </summary>
        public int Residue => buffer.Count - position;
        
        /// <summary>
        /// 将Blittable的数据进行内存拷贝
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe T Deserialize<T>() where T : unmanaged
        {
            T value;
            fixed (byte* ptr = &buffer.Array[buffer.Offset + position])
            {
#if UNITY_ANDROID
                T* valueBuffer = stackalloc T[1];
                UnsafeUtility.MemCpy(valueBuffer, ptr, sizeof(T));
                value = valueBuffer[0];
#else
                value = *(T*)ptr;
#endif
            }

            position += sizeof(T);
            return value;
        }

        /// <summary>
        /// 读取非托管的可空的类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T? DeserializeNone<T>() where T : unmanaged
        {
            return Deserialize<byte>() != 0 ? Deserialize<T>() : default(T?);
        }

        /// <summary>
        /// 内部的读取byte[]数组方法
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="count"></param>
        /// <returns>返回byte[]</returns>
        public byte[] ReadBytesInternal(byte[] bytes, int count)
        {
            if (count < 0)
            {
                Debug.LogError("传入长度需要大于0!");
            }

            if (count > bytes.Length)
            {
                Debug.LogError("长度不能大于数组大小!");
            }

            if (Residue < count)
            {
                Debug.LogError($"读取器剩余容量不够!{ToString()}");
            }

            Array.Copy(buffer.Array, buffer.Offset + position, bytes, 0, count);
            position += count;
            return bytes;
        }

        /// <summary>
        /// 内部的读取ArraySegment数组方法
        /// </summary>
        /// <param name="count"></param>
        /// <returns>返回ArraySegment</returns>
        public ArraySegment<byte> ReadArraySegmentInternal(int count)
        {
            if (count < 0)
            {
                Debug.LogError("传入长度需要大于0!");
            }

            if (Residue < count)
            {
                Debug.LogError($"读取器剩余容量不够!{ToString()}");
            }

            var segment = new ArraySegment<byte>(buffer.Array, buffer.Offset + position, count);
            position += count;
            return segment;
        }

        /// <summary>
        /// 读取自动生成的委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>()
        {
            var reader = Reader<T>.read;
            if (reader == null)
            {
                Debug.LogError($"无法获取读取器。读取器类型：{typeof(T)}.");
                return default;
            }

            return reader(this);
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
        public void Dispose()
        {
            Push(this);
        }
    }
}