using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ReSharper disable All
namespace JFramework.Net
{
    public static class NetworkReaderExtensions
    {
        public static byte ReadByte(this NetworkReader reader)
        {
            return reader.ReadBlittable<byte>();
        }

        public static sbyte ReadSByte(this NetworkReader reader)
        {
            return reader.ReadBlittable<sbyte>();
        }
        
        public static char ReadChar(this NetworkReader reader)
        {
            return (char)reader.ReadBlittable<ushort>();
        }

        public static bool ReadBool(this NetworkReader reader)
        {
            return reader.ReadBlittable<byte>() != 0;
        }

        public static short ReadShort(this NetworkReader reader)
        {
            return (short)reader.ReadUShort();
        }

        public static ushort ReadUShort(this NetworkReader reader)
        {
            return reader.ReadBlittable<ushort>();
        }
        
        public static int ReadInt(this NetworkReader reader)
        {
            return reader.ReadBlittable<int>();
        }

        public static uint ReadUInt(this NetworkReader reader)
        {
            return reader.ReadBlittable<uint>();
        }
        
        public static long ReadLong(this NetworkReader reader)
        {
            return reader.ReadBlittable<long>();
        }

        public static ulong ReadULong(this NetworkReader reader)
        {
            return reader.ReadBlittable<ulong>();
        }
        
        public static float ReadFloat(this NetworkReader reader)
        {
            return reader.ReadBlittable<float>();
        }
        
        public static double ReadDouble(this NetworkReader reader)
        {
            return reader.ReadBlittable<double>();
        }
        
        public static decimal ReadDecimal(this NetworkReader reader)
        {
            return reader.ReadBlittable<decimal>();
        }

        public static string ReadString(this NetworkReader reader)
        {
            ushort size = reader.ReadUShort();
            if (size == 0) return null;
            ushort realSize = (ushort)(size - 1);

            if (realSize > NetworkConst.MaxStringLength)
            {
                throw new EndOfStreamException($"Value too long: {realSize} bytes. Limit is: {NetworkConst.MaxStringLength} bytes");
            }

            ArraySegment<byte> data = reader.ReadBytesSegment(realSize);
            return reader.encoding.GetString(data.Array, data.Offset, data.Count);
        }
        
        public static ArraySegment<byte> ReadBytesAndSizeSegment(this NetworkReader reader)
        {
            uint count = reader.ReadUInt();
            return count == 0 ? default : reader.ReadBytesSegment(checked((int)(count - 1u)));
        }
        
        public static byte[] ReadBytesAndSize(this NetworkReader reader)
        {
            uint count = reader.ReadUInt();
            return count == 0 ? null : reader.ReadBytes(checked((int)(count - 1u)));
        }

        public static byte[] ReadBytes(this NetworkReader reader, int count)
        {
            byte[] bytes = new byte[count];
            reader.ReadBytes(bytes, count);
            return bytes;
        }

        public static Vector2 ReadVector2(this NetworkReader reader)
        {
            return reader.ReadBlittable<Vector2>();
        }
        
        public static Vector3 ReadVector3(this NetworkReader reader)
        {
            return reader.ReadBlittable<Vector3>();
        }

        public static Vector4 ReadVector4(this NetworkReader reader)
        {
            return reader.ReadBlittable<Vector4>();
        }
        
        public static Vector2Int ReadVector2Int(this NetworkReader reader)
        {
            return reader.ReadBlittable<Vector2Int>();
        }
        
        public static Vector3Int ReadVector3Int(this NetworkReader reader)
        {
            return reader.ReadBlittable<Vector3Int>();
        }
        
        public static Color ReadColor(this NetworkReader reader)
        {
            return reader.ReadBlittable<Color>();
        }

        public static Color32 ReadColor32(this NetworkReader reader)
        {
            return reader.ReadBlittable<Color32>();
        }
        
        public static Quaternion ReadQuaternion(this NetworkReader reader)
        {
            return reader.ReadBlittable<Quaternion>();
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
            return reader.ReadBlittable<Matrix4x4>();
        }
        
        public static Guid ReadGuid(this NetworkReader reader)
        {
            if (reader.Remaining >= 16)
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(reader.buffer.Array, reader.buffer.Offset + reader.position, 16);
                reader.position += 16;
                return new Guid(span);
            }
            throw new EndOfStreamException($"ReadGuid out of range: {reader}");
        }
   
        public static NetworkIdentity ReadNetworkIdentity(this NetworkReader reader)
        {
            uint netId = reader.ReadUInt();
            if (netId == 0) return null;
            return NetworkUtils.GetNetworkIdentity(netId);
        }

        public static NetworkBehaviour ReadNetworkBehaviour(this NetworkReader reader)
        {
            uint netId = reader.ReadUInt();
            if (netId == 0) return null;
            byte componentIndex = reader.ReadByte();
            NetworkIdentity identity = NetworkUtils.GetNetworkIdentity(netId);
            return identity != null ? identity.objects[componentIndex] : null;
        }

        public static T ReadNetworkBehaviour<T>(this NetworkReader reader) where T : NetworkBehaviour
        {
            return reader.ReadNetworkBehaviour() as T;
        }

        public static NetworkVariable ReadNetworkBehaviourSyncVar(this NetworkReader reader)
        {
            uint netId = reader.ReadUInt();
            byte componentIndex = default;
            if (netId != 0)
            {
                componentIndex = reader.ReadByte();
            }

            return new NetworkVariable(netId, componentIndex);
        }

        public static Transform ReadTransform(this NetworkReader reader)
        {
            NetworkIdentity networkIdentity = reader.ReadNetworkIdentity();
            return networkIdentity != null ? networkIdentity.transform : null;
        }

        public static GameObject ReadGameObject(this NetworkReader reader)
        {
            NetworkIdentity networkIdentity = reader.ReadNetworkIdentity();
            return networkIdentity != null ? networkIdentity.gameObject : null;
        }
        
        public static List<T> ReadList<T>(this NetworkReader reader)
        {
            int length = reader.ReadInt();
            if (length < 0)
                return null;
            List<T> result = new List<T>(length);
            for (int i = 0; i < length; i++)
            {
                result.Add(reader.Read<T>());
            }
            return result;
        }

        public static T[] ReadArray<T>(this NetworkReader reader)
        {
            int length = reader.ReadInt();
            if (length < 0) return null;
            if (length > reader.Remaining)
            {
                throw new EndOfStreamException($"Received array that is too large: {length}");
            }

            T[] result = new T[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = reader.Read<T>();
            }
            return result;
        }

        public static Uri ReadUri(this NetworkReader reader)
        {
            string uriString = reader.ReadString();
            return (string.IsNullOrWhiteSpace(uriString) ? null : new Uri(uriString));
        }

        public static Texture2D ReadTexture2D(this NetworkReader reader)
        {
            short width = reader.ReadShort();
            if (width == -1) return null;
            short height = reader.ReadShort();
            Texture2D texture2D = new Texture2D(width, height);
            Color32[] pixels = reader.ReadArray<Color32>();
            texture2D.SetPixels32(pixels);
            texture2D.Apply();
            return texture2D;
        }

        public static Sprite ReadSprite(this NetworkReader reader)
        {
            Texture2D texture = reader.ReadTexture2D();
            return texture == null ? null : Sprite.Create(texture, reader.ReadRect(), reader.ReadVector2());
        }

        public static DateTime ReadDateTime(this NetworkReader reader)
        {
            return DateTime.FromOADate(reader.ReadDouble());
        }
    }
}