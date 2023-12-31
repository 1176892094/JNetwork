using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable All
namespace JFramework.Net
{
    public static partial class StreamExtensions
    {
        public static void WriteByte(this NetworkWriter writer, byte value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteSByte(this NetworkWriter writer, sbyte value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteChar(this NetworkWriter writer, char value)
        {
            writer.WriteBlittable((ushort)value);
        }

        public static void WriteBool(this NetworkWriter writer, bool value)
        {
            writer.WriteBlittable((byte)(value ? 1 : 0));
        }

        public static void WriteShort(this NetworkWriter writer, short value)
        {
            writer.WriteBlittable(value);
        }
        
        public static void WriteUShort(this NetworkWriter writer, ushort value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteInt(this NetworkWriter writer, int value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteUInt(this NetworkWriter writer, uint value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteLong(this NetworkWriter writer, long value)
        {
            writer.WriteBlittable(value);
        }
        
        public static void WriteULong(this NetworkWriter writer, ulong value)
        {
            writer.WriteBlittable(value);
        }
        
        public static void WriteFloat(this NetworkWriter writer, float value)
        {
            writer.WriteBlittable(value);
        }
        
        public static void WriteDouble(this NetworkWriter writer, double value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteDecimal(this NetworkWriter writer, decimal value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteString(this NetworkWriter writer, string value)
        {
            if (value == null)
            {
                writer.WriteUShort(0);
                return;
            }
            
            int maxSize = writer.encoding.GetMaxByteCount(value.Length);
            writer.EnsureCapacity(writer.position + 2 + maxSize); 
            
            int written = writer.encoding.GetBytes(value, 0, value.Length, writer.buffer, writer.position + 2);
            
            if (written > NetworkConst.MaxStringLength)
            {
                throw new IndexOutOfRangeException($"String value too long: {written} > {NetworkConst.MaxStringLength}");
            }
            
            writer.WriteUShort(checked((ushort)(written + 1)));
            writer.position += written;
        }

        public static void WriteArraySegment(this NetworkWriter writer, ArraySegment<byte> buffer)
        {
            WriteBytes(writer, buffer.Array, buffer.Offset, buffer.Count);
        }
        
        public static void WriteBytes(this NetworkWriter writer, byte[] buffer)
        {
            WriteBytes(writer, buffer, 0, buffer?.Length ?? 0);
        }
        
        public static void WriteBytes(this NetworkWriter writer, byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                writer.WriteUInt(0u);
                return;
            }
            writer.WriteUInt(checked((uint)count) + 1U);
            writer.WriteBytesInternal(buffer, offset, count);
        }

        public static void WriteArraySegment<T>(this NetworkWriter writer, ArraySegment<T> segment)
        {
            int length = segment.Count;
            writer.WriteInt(length);
            for (int i = 0; i < length; i++)
            {
                writer.Write(segment.Array[segment.Offset + i]);
            }
        }

        public static void WriteVector2(this NetworkWriter writer, Vector2 value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteVector3(this NetworkWriter writer, Vector3 value)
        {
            writer.WriteBlittable(value);
        }
        
        public static void WriteVector3Nullable(this NetworkWriter writer, Vector3? value)
        {
            writer.WriteBlittableNullable(value);
        }

        public static void WriteVector4(this NetworkWriter writer, Vector4 value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteVector2Int(this NetworkWriter writer, Vector2Int value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteVector3Int(this NetworkWriter writer, Vector3Int value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteColor(this NetworkWriter writer, Color value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteColor32(this NetworkWriter writer, Color32 value)
        {
            writer.WriteBlittable(value);
        }
        public static void WriteQuaternion(this NetworkWriter writer, Quaternion value)
        {
            writer.WriteBlittable(value);
        }
        
        public static void WriteQuaternionNullable(this NetworkWriter writer, Quaternion? value)
        {
            writer.WriteBlittableNullable(value);
        }

        public static void WriteRect(this NetworkWriter writer, Rect value)
        {
            writer.WriteVector2(value.position);
            writer.WriteVector2(value.size);
        }

        public static void WritePlane(this NetworkWriter writer, Plane value)
        {
            writer.WriteVector3(value.normal);
            writer.WriteFloat(value.distance);
        }

        public static void WriteRay(this NetworkWriter writer, Ray value)
        {
            writer.WriteVector3(value.origin);
            writer.WriteVector3(value.direction);
        }

        public static void WriteMatrix4x4(this NetworkWriter writer, Matrix4x4 value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteGuid(this NetworkWriter writer, Guid value)
        {
            writer.EnsureCapacity(writer.position + 16);
            value.TryWriteBytes(new Span<byte>(writer.buffer, writer.position, 16));
            writer.position += 16;
        }

        public static void WriteNetworkObject(this NetworkWriter writer, NetworkObject value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }
            
            if (value.objectId == 0)
            {
                Debug.LogWarning($"NetworkObject 的Id为零。\n");
            }

            writer.WriteUInt(value.objectId);
        }

        public static void WriteNetworkBehaviour(this NetworkWriter writer, NetworkBehaviour value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }
            
            if (value.objectId == 0)
            {
                Debug.LogWarning($"NetworkObject 的Id为零。\n");
                writer.WriteUInt(0);
                return;
            }

            writer.WriteUInt(value.objectId);
            writer.WriteByte(value.serialId);
        }

        public static void WriteTransform(this NetworkWriter writer, Transform value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }
            if (value.TryGetComponent(out NetworkObject @object))
            {
                writer.WriteUInt(@object.objectId);
            }
            else
            {
                Debug.LogWarning($"Transform {value} 没有 NetworkObject 组件");
                writer.WriteUInt(0);
            }
        }

        public static void WriteGameObject(this NetworkWriter writer, GameObject value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }
            
            if (!value.TryGetComponent(out NetworkObject @object))
            {
                Debug.LogWarning($"GameObject {value} 没有 NetworkObject 组件");
            }
            
            writer.WriteNetworkObject(@object);
        }
        
        public static void WriteList<T>(this NetworkWriter writer, List<T> list)
        {
            if (list is null)
            {
                writer.WriteInt(-1);
                return;
            }
            writer.WriteInt(list.Count);
            foreach (var obj in list)
            {
                writer.Write(obj);
            }
        }

        public static void WriteArray<T>(this NetworkWriter writer, T[] array)
        {
            if (array is null)
            {
                writer.WriteInt(-1);
                return;
            }
            writer.WriteInt(array.Length);
            foreach (var obj in array)
            {
                writer.Write(obj);
            }
        }

        public static void WriteUri(this NetworkWriter writer, Uri uri)
        {
            writer.WriteString(uri?.ToString());
        }

        public static void WriteTexture2D(this NetworkWriter writer, Texture2D texture2D)
        {
            if (texture2D == null)
            {
                writer.WriteShort(-1);
                return;
            }
            
            writer.WriteShort((short)texture2D.width);
            writer.WriteShort((short)texture2D.height);
            writer.WriteArray(texture2D.GetPixels32());
        }

        public static void WriteSprite(this NetworkWriter writer, Sprite sprite)
        {
            if (sprite == null)
            {
                writer.WriteTexture2D(null);
                return;
            }

            writer.WriteTexture2D(sprite.texture);
            writer.WriteRect(sprite.rect);
            writer.WriteVector2(sprite.pivot);
        }

        public static void WriteDateTime(this NetworkWriter writer, DateTime dateTime)
        {
            writer.WriteDouble(dateTime.ToOADate());
        }
    }
}
