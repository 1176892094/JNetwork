using System;
using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class StreamExtensions
    {
        public static void WriteByte(this NetworkWriter writer, byte value)
        {
            writer.Serialize(value);
        }

        public static void WriteSByte(this NetworkWriter writer, sbyte value)
        {
            writer.Serialize(value);
        }

        public static void WriteChar(this NetworkWriter writer, char value)
        {
            writer.Serialize((ushort)value);
        }

        public static void WriteBool(this NetworkWriter writer, bool value)
        {
            writer.Serialize((byte)(value ? 1 : 0));
        }

        public static void WriteShort(this NetworkWriter writer, short value)
        {
            writer.Serialize(value);
        }

        public static void WriteUShort(this NetworkWriter writer, ushort value)
        {
            writer.Serialize(value);
        }

        public static void WriteInt(this NetworkWriter writer, int value)
        {
            writer.Serialize(value);
        }

        public static void WriteUInt(this NetworkWriter writer, uint value)
        {
            writer.Serialize(value);
        }

        public static void WriteLong(this NetworkWriter writer, long value)
        {
            writer.Serialize(value);
        }

        public static void WriteULong(this NetworkWriter writer, ulong value)
        {
            writer.Serialize(value);
        }

        public static void WriteFloat(this NetworkWriter writer, float value)
        {
            writer.Serialize(value);
        }

        public static void WriteDouble(this NetworkWriter writer, double value)
        {
            writer.Serialize(value);
        }

        public static void WriteDecimal(this NetworkWriter writer, decimal value)
        {
            writer.Serialize(value);
        }

        public static void WriteString(this NetworkWriter writer, string value)
        {
            if (value == null)
            {
                writer.WriteUShort(0);
                return;
            }

            var size = writer.encoding.GetMaxByteCount(value.Length);
            writer.EnsureCapacity(writer.position + 2 + size);
            var bytes = writer.encoding.GetBytes(value, 0, value.Length, writer.buffer, writer.position + 2);
            if (bytes > NetworkConst.MaxStringLength)
            {
                throw new IndexOutOfRangeException("写入字符串过长!");
            }

            writer.WriteUShort(checked((ushort)(bytes + 1)));
            writer.position += bytes;
        }

        public static void WriteArraySegment(this NetworkWriter writer, ArraySegment<byte> buffer)
        {
            if (buffer == null)
            {
                writer.WriteUInt(0);
                return;
            }

            writer.WriteUInt(checked((uint)buffer.Count) + 1);
            writer.WriteBytesInternal(buffer.Array, buffer.Offset, buffer.Count);
        }

        public static void WriteBytes(this NetworkWriter writer, byte[] buffer)
        {
            if (buffer == null)
            {
                writer.WriteUInt(0);
                return;
            }

            writer.WriteUInt(checked((uint)buffer.Length) + 1);
            writer.WriteBytesInternal(buffer, 0, buffer.Length);
        }

        public static void WriteArraySegment<T>(this NetworkWriter writer, ArraySegment<T> segment)
        {
            writer.WriteInt(segment.Count);
            for (int i = 0; i < segment.Count; i++)
            {
                writer.Write(segment.Array[segment.Offset + i]);
            }
        }

        public static void WriteVector2(this NetworkWriter writer, Vector2 value)
        {
            writer.Serialize(value);
        }

        public static void WriteVector3(this NetworkWriter writer, Vector3 value)
        {
            writer.Serialize(value);
        }

        public static void WriteVector3Nullable(this NetworkWriter writer, Vector3? value)
        {
            writer.SerializeNone(value);
        }

        public static void WriteVector4(this NetworkWriter writer, Vector4 value)
        {
            writer.Serialize(value);
        }

        public static void WriteVector2Int(this NetworkWriter writer, Vector2Int value)
        {
            writer.Serialize(value);
        }

        public static void WriteVector3Int(this NetworkWriter writer, Vector3Int value)
        {
            writer.Serialize(value);
        }

        public static void WriteColor(this NetworkWriter writer, Color value)
        {
            writer.Serialize(value);
        }

        public static void WriteColor32(this NetworkWriter writer, Color32 value)
        {
            writer.Serialize(value);
        }

        public static void WriteQuaternion(this NetworkWriter writer, Quaternion value)
        {
            writer.Serialize(value);
        }

        public static void WriteQuaternionNullable(this NetworkWriter writer, Quaternion? value)
        {
            writer.SerializeNone(value);
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
            writer.Serialize(value);
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
                Debug.LogWarning("NetworkObject的Id为0。\n");
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
                Debug.LogWarning("NetworkObject的Id为0。\n");
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

            if (!value.TryGetComponent(out NetworkObject @object))
            {
                Debug.LogWarning($"Transform {value} 没有 NetworkObject 组件");
                writer.WriteUInt(0);
                return;
            }

            writer.WriteUInt(@object.objectId);
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
                writer.WriteUInt(0);
                return;
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

        public static void WriteTexture2D(this NetworkWriter writer, Texture2D texture)
        {
            if (texture == null)
            {
                writer.WriteShort(-1);
                return;
            }

            writer.WriteShort((short)texture.width);
            writer.WriteShort((short)texture.height);
            writer.WriteArray(texture.GetPixels32());
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