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
        public static byte ReadByte(this NetworkReader reader)
        {
            return reader.Read<byte>();
        }

        public static sbyte ReadSByte(this NetworkReader reader)
        {
            return reader.Read<sbyte>();
        }

        public static char ReadChar(this NetworkReader reader)
        {
            return (char)reader.Read<ushort>();
        }

        public static bool ReadBool(this NetworkReader reader)
        {
            return reader.Read<byte>() != 0;
        }

        public static short ReadShort(this NetworkReader reader)
        {
            return reader.Read<short>();
        }

        public static ushort ReadUShort(this NetworkReader reader)
        {
            return reader.Read<ushort>();
        }

        public static int ReadInt(this NetworkReader reader)
        {
            return reader.Read<int>();
        }

        public static uint ReadUInt(this NetworkReader reader)
        {
            return reader.Read<uint>();
        }

        public static long ReadLong(this NetworkReader reader)
        {
            return reader.Read<long>();
        }

        public static ulong ReadULong(this NetworkReader reader)
        {
            return reader.Read<ulong>();
        }

        public static float ReadFloat(this NetworkReader reader)
        {
            return reader.Read<float>();
        }

        public static double ReadDouble(this NetworkReader reader)
        {
            return reader.Read<double>();
        }

        public static decimal ReadDecimal(this NetworkReader reader)
        {
            return reader.Read<decimal>();
        }

        public static string ReadString(this NetworkReader reader)
        {
            var count = reader.ReadUShort();
            if (count == 0)
            {
                return null;
            }

            count = (ushort)(count - 1);
            if (count > ushort.MaxValue - 1)
            {
                throw new EndOfStreamException("读取字符串过长!");
            }

            var segment = reader.ReadArraySegment(count);
            return reader.encoding.GetString(segment.Array, segment.Offset, segment.Count);
        }

        public static byte[] ReadBytes(this NetworkReader reader)
        {
            var count = reader.ReadUInt();
            return count == 0 ? null : reader.ReadArraySegment(checked((int)(count - 1))).Array;
        }
        
        public static ArraySegment<byte> ReadArraySegment(this NetworkReader reader)
        {
            var count = reader.ReadUInt();
            return count == 0 ? null : reader.ReadArraySegment(checked((int)(count - 1)));
        }

        public static Vector2 ReadVector2(this NetworkReader reader)
        {
            return reader.Read<Vector2>();
        }

        public static Vector3 ReadVector3(this NetworkReader reader)
        {
            return reader.Read<Vector3>();
        }

        public static Vector3? ReadVector3Nullable(this NetworkReader reader)
        {
            return reader.ReadEmpty<Vector3>();
        }

        public static Vector4 ReadVector4(this NetworkReader reader)
        {
            return reader.Read<Vector4>();
        }

        public static Vector2Int ReadVector2Int(this NetworkReader reader)
        {
            return reader.Read<Vector2Int>();
        }

        public static Vector3Int ReadVector3Int(this NetworkReader reader)
        {
            return reader.Read<Vector3Int>();
        }

        public static Color ReadColor(this NetworkReader reader)
        {
            return reader.Read<Color>();
        }

        public static Color32 ReadColor32(this NetworkReader reader)
        {
            return reader.Read<Color32>();
        }

        public static Quaternion ReadQuaternion(this NetworkReader reader)
        {
            return reader.Read<Quaternion>();
        }

        public static Quaternion? ReadQuaternionNullable(this NetworkReader reader)
        {
            return reader.ReadEmpty<Quaternion>();
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
            return reader.Read<Matrix4x4>();
        }
        
        public static Guid ReadGuid(this NetworkReader reader)
        {
            if (reader.residue < 16)
            {
                throw new OverflowException("读取器剩余容量不够!");
            }

            var span = new Span<byte>(reader.buffer.Array, reader.buffer.Offset + reader.position, 16);
            reader.position += 16;
            return new Guid(span);
        }

        public static DateTime ReadDateTime(this NetworkReader reader)
        {
            return DateTime.FromOADate(reader.ReadDouble());
        }
        
        public static List<T> ReadList<T>(this NetworkReader reader)
        {
            var length = reader.ReadInt();
            if (length < 0)
            {
                return null;
            }

            var result = new List<T>(length);
            for (int i = 0; i < length; i++)
            {
                result.Add(reader.Invoke<T>());
            }

            return result;
        }

        public static T[] ReadArray<T>(this NetworkReader reader)
        {
            var length = reader.ReadInt();
            if (length < 0)
            {
                return null;
            }

            var result = new T[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.Invoke<T>();
            }

            return result;
        }

        public static Uri ReadUri(this NetworkReader reader)
        {
            var uri = reader.ReadString();
            return string.IsNullOrWhiteSpace(uri) ? null : new Uri(uri);
        }

        public static NetworkObject ReadNetworkObject(this NetworkReader reader)
        {
            var objectId = reader.ReadUInt();
            return objectId != 0 ? NetworkUtility.GetNetworkObject(objectId) : null;
        }

        public static NetworkBehaviour ReadNetworkBehaviour(this NetworkReader reader)
        {
            var @object = reader.ReadNetworkObject();
            return @object != null ? @object.entities[reader.ReadByte()] : null;
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

        public static Texture2D ReadTexture2D(this NetworkReader reader)
        {
            var width = reader.ReadShort();
            if (width < 0)
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
    }

    public static partial class StreamExtensions
    {
        public static T ReadNetworkBehaviour<T>(this NetworkReader reader) where T : NetworkBehaviour
        {
            return reader.ReadNetworkBehaviour() as T;
        }

        public static NetworkVariable ReadNetworkVariable(this NetworkReader reader)
        {
            uint objectId = reader.ReadUInt();
            byte componentId = default;
            
            if (objectId != 0)
            {
                componentId = reader.ReadByte();
            }

            return new NetworkVariable(objectId, componentId);
        }
    }
}