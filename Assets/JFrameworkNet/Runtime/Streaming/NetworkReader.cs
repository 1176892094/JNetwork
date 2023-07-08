using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

// ReSharper disable All
namespace JFramework.Net
{
    public class NetworkReader: IDisposable
    {
        /// <summary>
        /// 字符串编码
        /// </summary>
        internal readonly UTF8Encoding encoding = new UTF8Encoding(false, true);
        
        /// <summary>
        /// 缓存的字节数组
        /// </summary>
        internal ArraySegment<byte> buffer;
        
        /// <summary>
        /// 当前字节数组中的位置
        /// </summary>
        public int position;
        
        /// <summary>
        /// 剩余长度
        /// </summary>
        public int Residue => buffer.Count - position;
        
        /// <summary>
        /// 当前容量
        /// </summary>
        public int Capacity => buffer.Count;

        public NetworkReader() => buffer = new ArraySegment<byte>();
        
        /// <summary>
        /// 拷贝ArraySegment到缓存中
        /// </summary>
        /// <param name="segment"></param>
        public NetworkReader(ArraySegment<byte> segment) => buffer = segment;

        /// <summary>
        /// 设置缓存数组
        /// </summary>
        /// <param name="segment"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBuffer(ArraySegment<byte> segment)
        {
            buffer = segment;
            position = 0;
        }
        
        /// <summary>
        /// 将Blittable的数据进行内存拷贝
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe T ReadBlittable<T>() where T : unmanaged
        {
#if UNITY_EDITOR
            if (!UnsafeUtility.IsBlittable(typeof(T)))
            {
                throw new ArgumentException($"{typeof(T)} is not blittable!");
            }
#endif
            
            int size = sizeof(T);
            
            if (Residue < size)
            {
                throw new EndOfStreamException($"ReadBlittable<{typeof(T)}> not enough data in buffer to read {size} bytes: {ToString()}");
            }

            T value;
            fixed (byte* ptr = & buffer.Array[buffer.Offset + position])
            {
#if UNITY_ANDROID
                T* valueBuffer = stackalloc T[1];
                UnsafeUtility.MemCpy(valueBuffer, ptr, size);
                value = valueBuffer[0];
#else
                value = *(T*)ptr;
#endif
            }
            position += size;
            return value;
        }
        

        public byte ReadByte() => ReadBlittable<byte>();
        
        public byte[] ReadBytesInternal(byte[] bytes, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("ReadBytes requires count >= 0");
            }

            if (count > bytes.Length)
            {
                throw new EndOfStreamException($"ReadBytes can't read {count} + bytes because the passed byte[] only has length {bytes.Length}");
            }
          
            if (Residue < count)
            {
                throw new EndOfStreamException($"ReadBytesSegment can't read {count} bytes because it would read past the end of the stream. {ToString()}");
            }

            Array.Copy(buffer.Array, buffer.Offset + position, bytes, 0, count);
            position += count;
            return bytes;
        }
        
        public ArraySegment<byte> ReadArraySegmentInternal(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("ReadBytesSegment requires count >= 0");
            }
            
            if (Residue < count)
            {
                throw new EndOfStreamException($"ReadBytesSegment can't read {count} bytes because it would read past the end of the stream. {ToString()}");
            }
            
            ArraySegment<byte> result = new ArraySegment<byte>(buffer.Array, buffer.Offset + position, count);
            position += count;
            return result;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>()
        {
            Func<NetworkReader, T> readerDelegate = Reader<T>.read;
            if (readerDelegate == null)
            {
                Debug.LogError($"No reader found for {typeof(T)}.");
                return default;
            }
            return readerDelegate(this);
        }

        public override string ToString()
        {
            return buffer.Array != null ? BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count) : null;
        }

        public void Dispose() => NetworkReaderPool.Push(this);
    }
    
    public static class Reader<T>
    {
        public static Func<NetworkReader, T> read;
    }
}
