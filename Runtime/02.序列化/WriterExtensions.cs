// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  04:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class StreamExtensions
    {
        public static void WriteByte(this NetworkWriter writer, byte value)
        {
            writer.Write(value);
        }

        public static void WriteSByte(this NetworkWriter writer, sbyte value)
        {
            writer.Write(value);
        }

        public static void WriteChar(this NetworkWriter writer, char value)
        {
            writer.Write((ushort)value);
        }

        public static void WriteBool(this NetworkWriter writer, bool value)
        {
            writer.Write((byte)(value ? 1 : 0));
        }

        public static void WriteShort(this NetworkWriter writer, short value)
        {
            writer.Write(value);
        }

        public static void WriteUShort(this NetworkWriter writer, ushort value)
        {
            writer.Write(value);
        }

        public static void WriteInt(this NetworkWriter writer, int value)
        {
            writer.Write(value);
        }

        public static void WriteUInt(this NetworkWriter writer, uint value)
        {
            writer.Write(value);
        }

        public static void WriteLong(this NetworkWriter writer, long value)
        {
            writer.Write(value);
        }

        public static void WriteULong(this NetworkWriter writer, ulong value)
        {
            writer.Write(value);
        }

        public static void WriteFloat(this NetworkWriter writer, float value)
        {
            writer.Write(value);
        }

        public static void WriteDouble(this NetworkWriter writer, double value)
        {
            writer.Write(value);
        }

        public static void WriteDecimal(this NetworkWriter writer, decimal value)
        {
            writer.Write(value);
        }

        public static void WriteString(this NetworkWriter writer, string value)
        {
            if (value == null)
            {
                writer.WriteUShort(0);
                return;
            }

            writer.AddCapacity(writer.position + 2 + writer.encoding.GetMaxByteCount(value.Length));
            var count = writer.encoding.GetBytes(value, 0, value.Length, writer.buffer, writer.position + 2);
            if (count > ushort.MaxValue - 1)
            {
                throw new EndOfStreamException("写入字符串过长!");
            }

            writer.WriteUShort(checked((ushort)(count + 1))); // writer.position + 2
            writer.position += count;
        }

        public static void WriteBytes(this NetworkWriter writer, byte[] value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }

            writer.WriteUInt(checked((uint)value.Length) + 1);
            writer.WriteBytes(value, 0, value.Length);
        }

        public static void WriteArraySegment(this NetworkWriter writer, ArraySegment<byte> value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }

            writer.WriteUInt(checked((uint)value.Count) + 1);
            writer.WriteBytes(value.Array, value.Offset, value.Count);
        }

        public static void WriteVector2(this NetworkWriter writer, Vector2 value)
        {
            writer.Write(value);
        }

        public static void WriteVector3(this NetworkWriter writer, Vector3 value)
        {
            writer.Write(value);
        }

        public static void WriteVector3Nullable(this NetworkWriter writer, Vector3? value)
        {
            writer.WriteEmpty(value);
        }

        public static void WriteVector4(this NetworkWriter writer, Vector4 value)
        {
            writer.Write(value);
        }

        public static void WriteVector2Int(this NetworkWriter writer, Vector2Int value)
        {
            writer.Write(value);
        }

        public static void WriteVector3Int(this NetworkWriter writer, Vector3Int value)
        {
            writer.Write(value);
        }

        public static void WriteColor(this NetworkWriter writer, Color value)
        {
            writer.Write(value);
        }

        public static void WriteColor32(this NetworkWriter writer, Color32 value)
        {
            writer.Write(value);
        }

        public static void WriteQuaternion(this NetworkWriter writer, Quaternion value)
        {
            writer.Write(value);
        }

        public static void WriteQuaternionNullable(this NetworkWriter writer, Quaternion? value)
        {
            writer.WriteEmpty(value);
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
            writer.Write(value);
        }

        public static void WriteGuid(this NetworkWriter writer, Guid value)
        {
            writer.AddCapacity(writer.position + 16);
            value.TryWriteBytes(new Span<byte>(writer.buffer, writer.position, 16));
            writer.position += 16;
        }

        public static void WriteDateTime(this NetworkWriter writer, DateTime value)
        {
            writer.WriteDouble(value.ToOADate());
        }

        public static void WriteList<T>(this NetworkWriter writer, List<T> values)
        {
            if (values == null)
            {
                writer.WriteInt(-1);
                return;
            }

            writer.WriteInt(values.Count);
            foreach (var value in values)
            {
                writer.Invoke(value);
            }
        }

        public static void WriteArray<T>(this NetworkWriter writer, T[] values)
        {
            if (values == null)
            {
                writer.WriteInt(-1);
                return;
            }

            writer.WriteInt(values.Length);
            foreach (var value in values)
            {
                writer.Invoke(value);
            }
        }

        public static void WriteUri(this NetworkWriter writer, Uri value)
        {
            if (value == null)
            {
                writer.WriteString(null);
                return;
            }

            writer.WriteString(value.ToString());
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
                Debug.LogWarning("NetworkObject的对象索引为0。\n");
                writer.WriteUInt(0);
                return;
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

            writer.WriteNetworkObject(value.@object);
            writer.WriteByte(value.componentId);
        }

        public static void WriteTransform(this NetworkWriter writer, Transform value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }

            writer.WriteNetworkObject(value.GetComponent<NetworkObject>());
        }

        public static void WriteGameObject(this NetworkWriter writer, GameObject value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }

            writer.WriteNetworkObject(value.GetComponent<NetworkObject>());
        }

        public static void WriteTexture2D(this NetworkWriter writer, Texture2D value)
        {
            if (value == null)
            {
                writer.WriteShort(-1);
                return;
            }

            writer.WriteShort((short)value.width);
            writer.WriteShort((short)value.height);
            writer.WriteArray(value.GetPixels32());
        }

        public static void WriteSprite(this NetworkWriter writer, Sprite value)
        {
            if (value == null)
            {
                writer.WriteTexture2D(null);
                return;
            }

            writer.WriteTexture2D(value.texture);
            writer.WriteRect(value.rect);
            writer.WriteVector2(value.pivot);
        }
    }

    public static partial class StreamExtensions
    {
        public static void WriteArraySegment<T>(this NetworkWriter writer, ArraySegment<T> value)
        {
            writer.WriteInt(value.Count);
            for (int i = 0; i < value.Count; i++)
            {
                writer.Invoke(value.Array[value.Offset + i]);
            }
        }
    }
}