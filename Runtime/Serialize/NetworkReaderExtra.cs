using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace JFramework.Net
{
    public static partial class StreamExtensions
    {
        public static byte ReadByte(this NetworkReader reader)
        {
            return reader.Deserialize<byte>();
        }

        public static sbyte ReadSByte(this NetworkReader reader)
        {
            return reader.Deserialize<sbyte>();
        }

        public static char ReadChar(this NetworkReader reader)
        {
            return (char)reader.Deserialize<ushort>();
        }

        public static bool ReadBool(this NetworkReader reader)
        {
            return reader.Deserialize<byte>() != 0;
        }

        public static short ReadShort(this NetworkReader reader)
        {
            return reader.Deserialize<short>();
        }

        public static ushort ReadUShort(this NetworkReader reader)
        {
            return reader.Deserialize<ushort>();
        }

        public static int ReadInt(this NetworkReader reader)
        {
            return reader.Deserialize<int>();
        }

        public static uint ReadUInt(this NetworkReader reader)
        {
            return reader.Deserialize<uint>();
        }

        public static long ReadLong(this NetworkReader reader)
        {
            return reader.Deserialize<long>();
        }

        public static ulong ReadULong(this NetworkReader reader)
        {
            return reader.Deserialize<ulong>();
        }

        public static float ReadFloat(this NetworkReader reader)
        {
            return reader.Deserialize<float>();
        }

        public static double ReadDouble(this NetworkReader reader)
        {
            return reader.Deserialize<double>();
        }

        public static decimal ReadDecimal(this NetworkReader reader)
        {
            return reader.Deserialize<decimal>();
        }

        public static string ReadString(this NetworkReader reader)
        {
            var size = reader.ReadUShort();
            if (size == 0)
            {
                return null;
            }

            size = (ushort)(size - 1);
            if (size > NetworkConst.MaxStringLength)
            {
                throw new EndOfStreamException("读取字符串过长!");
            }

            var segment = reader.ReadArraySegmentInternal(size);
            return reader.encoding.GetString(segment.Array, segment.Offset, segment.Count);
        }

        public static ArraySegment<byte> ReadArraySegment(this NetworkReader reader)
        {
            var count = reader.ReadUInt();
            return count == 0 ? default : reader.ReadArraySegmentInternal(checked((int)(count - 1)));
        }

        public static byte[] ReadBytes(this NetworkReader reader)
        {
            var count = reader.ReadUInt();
            return count == 0 ? null : reader.ReadBytes(checked((int)(count - 1)));
        }

        public static byte[] ReadBytes(this NetworkReader reader, int count)
        {
            var bytes = new byte[count];
            reader.ReadBytesInternal(bytes, count);
            return bytes;
        }

        public static Vector2 ReadVector2(this NetworkReader reader)
        {
            return reader.Deserialize<Vector2>();
        }

        public static Vector3 ReadVector3(this NetworkReader reader)
        {
            return reader.Deserialize<Vector3>();
        }

        public static Vector3? ReadVector3Nullable(this NetworkReader reader)
        {
            return reader.DeserializeNone<Vector3>();
        }

        public static Vector4 ReadVector4(this NetworkReader reader)
        {
            return reader.Deserialize<Vector4>();
        }

        public static Vector2Int ReadVector2Int(this NetworkReader reader)
        {
            return reader.Deserialize<Vector2Int>();
        }

        public static Vector3Int ReadVector3Int(this NetworkReader reader)
        {
            return reader.Deserialize<Vector3Int>();
        }

        public static Color ReadColor(this NetworkReader reader)
        {
            return reader.Deserialize<Color>();
        }

        public static Color32 ReadColor32(this NetworkReader reader)
        {
            return reader.Deserialize<Color32>();
        }

        public static Quaternion ReadQuaternion(this NetworkReader reader)
        {
            return reader.Deserialize<Quaternion>();
        }

        public static Quaternion? ReadQuaternionNullable(this NetworkReader reader)
        {
            return reader.DeserializeNone<Quaternion>();
        }

        public static Rect ReadRect(this NetworkReader reader)
        {
            return new Rect(reader.ReadVector2(), reader.ReadVector2());
        }

        public static Plane ReadPlane(this NetworkReader reader)
        {
            return new Plane(reader.ReadVector3(), reader.ReadFloat());
        }

        public static Ray ReadRay(this NetworkReader reader)
        {
            return new Ray(reader.ReadVector3(), reader.ReadVector3());
        }

        public static Matrix4x4 ReadMatrix4x4(this NetworkReader reader)
        {
            return reader.Deserialize<Matrix4x4>();
        }

        public static Guid ReadGuid(this NetworkReader reader)
        {
            if (reader.Residue >= 16)
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(reader.buffer.Array, reader.buffer.Offset + reader.position, 16);
                reader.position += 16;
                return new Guid(span);
            }

            throw new EndOfStreamException($"读取器剩余容量不够!{reader}");
        }

        public static NetworkObject ReadNetworkObject(this NetworkReader reader)
        {
            var id = reader.ReadUInt();
            return id == 0 ? null : NetworkUtils.GetNetworkObject(id);
        }

        public static NetworkBehaviour ReadNetworkBehaviour(this NetworkReader reader)
        {
            uint id = reader.ReadUInt();
            if (id == 0)
            {
                return null;
            }

            var component = reader.ReadByte();
            var @object = NetworkUtils.GetNetworkObject(id);
            return @object != null ? @object.entities[component] : null;
        }

        public static T ReadNetworkBehaviour<T>(this NetworkReader reader) where T : NetworkBehaviour
        {
            return reader.ReadNetworkBehaviour() as T;
        }

        public static NetworkVariable ReadNetworkValue(this NetworkReader reader)
        {
            var id = reader.ReadUInt();
            byte component = 0;
            if (id != 0)
            {
                component = reader.ReadByte();
            }

            return new NetworkVariable(id, component);
        }

        public static Transform ReadTransform(this NetworkReader reader)
        {
            var @object = reader.ReadNetworkObject();
            return @object != null ? @object.transform : null;
        }

        public static GameObject ReadGameObject(this NetworkReader reader)
        {
            var @object = reader.ReadNetworkObject();
            return @object != null ? @object.gameObject : null;
        }

        public static List<T> ReadList<T>(this NetworkReader reader)
        {
            int length = reader.ReadInt();
            if (length < 0)
            {
                return null;
            }

            var result = new List<T>(length);
            for (int i = 0; i < length; i++)
            {
                result.Add(reader.Read<T>());
            }

            return result;
        }

        public static T[] ReadArray<T>(this NetworkReader reader)
        {
            int length = reader.ReadInt();
            if (length < 0)
            {
                return null;
            }

            var result = new T[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.Read<T>();
            }

            return result;
        }

        public static Uri ReadUri(this NetworkReader reader)
        {
            string uri = reader.ReadString();
            return string.IsNullOrWhiteSpace(uri) ? null : new Uri(uri);
        }

        public static Texture2D ReadTexture2D(this NetworkReader reader)
        {
            var width = reader.ReadShort();
            if (width == -1)
            {
                return null;
            }

            var height = reader.ReadShort();
            var texture = new Texture2D(width, height);
            var pixels = reader.ReadArray<Color32>();
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        public static Sprite ReadSprite(this NetworkReader reader)
        {
            var texture = reader.ReadTexture2D();
            return texture == null ? null : Sprite.Create(texture, reader.ReadRect(), reader.ReadVector2());
        }

        public static DateTime ReadDateTime(this NetworkReader reader)
        {
            return DateTime.FromOADate(reader.ReadDouble());
        }
    }
}