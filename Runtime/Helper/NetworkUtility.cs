// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  13:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkUtility
    {
        /// <summary>
        /// 根据名称获取Hash码
        /// </summary>
        /// <param name="name">传入名称</param>
        /// <returns>返回Hash码</returns>
        public static uint GetHashToName(string name)
        {
            return unchecked(name.Aggregate(23U, (i, c) => i * 31 + c));
        }

        /// <summary>
        /// 获取随机值
        /// </summary>
        /// <returns></returns>
        internal static uint GetRandomId()
        {
            using var provider = new RNGCryptoServiceProvider();
            var buffer = new byte[4];
            provider.GetBytes(buffer);
            return BitConverter.ToUInt32(buffer);
        }

        /// <summary>
        /// 获取地址
        /// </summary>
        /// <returns></returns>
        public static string GetHostName()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var inter in interfaces)
                {
                    if (inter.OperationalStatus == OperationalStatus.Up && inter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        var properties = inter.GetIPProperties();
                        var ip = properties.UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork);
                        if (ip != null)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }

                // 虚拟机无法解析网络接口 因此额外解析主机地址
                var addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                foreach (var ip in addresses)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }

                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// 是否为场景网络对象
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        public static bool IsSceneObject(NetworkObject @object)
        {
            if (@object.sceneId == 0)
            {
                return false;
            }

            if (@object.gameObject.hideFlags == HideFlags.NotEditable)
            {
                return false;
            }

            return @object.gameObject.hideFlags != HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// 获取网络对象
        /// </summary>
        /// <param name="objectId">传入网络Id</param>
        /// <returns>返回网络对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkObject GetNetworkObject(uint objectId)
        {
            if (NetworkManager.Server.isActive)
            {
                NetworkManager.Server.spawns.TryGetValue(objectId, out var @object);
                return @object;
            }

            if (NetworkManager.Client.isActive)
            {
                NetworkManager.Client.spawns.TryGetValue(objectId, out var @object);
                return @object;
            }

            return null;
        }

        /// <summary>
        /// 发送消息是否有效
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(ArraySegment<byte> segment, int channel)
        {
            if (segment.Count == 0)
            {
                Debug.LogError("发送消息大小不能为零！");
                return false;
            }

            if (segment.Count > NetworkManager.Transport.MessageSize(channel))
            {
                Debug.LogError($"发送消息大小过大！消息大小：{segment.Count}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 注册网络消息委托
        /// </summary>
        /// <param name="action">传入网络连接，网络消息，传输通道</param>
        /// <typeparam name="T">网络消息</typeparam>
        /// <returns>返回一个消息委托</returns>
        internal static MessageDelegate GetMessage<T>(Action<NetworkClient, T, int> action) where T : struct, Message
        {
            return (client, reader, channel) =>
            {
                try
                {
                    var message = reader.Invoke<T>();
                    action?.Invoke(client, message, channel);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{typeof(T).Name} 调用失败。传输通道: {channel}\n" + e);
                    client.Disconnect();
                }
            };
        }

        /// <summary>
        /// 注册网络消息委托
        /// </summary>
        /// <param name="action">传入网络连接，网络消息</param>
        /// <typeparam name="T">网络消息</typeparam>
        /// <returns>返回一个消息委托</returns>
        internal static MessageDelegate GetMessage<T>(Action<NetworkClient, T> action) where T : struct, Message
        {
            return (client, reader, channel) =>
            {
                try
                {
                    var message = reader.Invoke<T>();
                    action?.Invoke(client, message);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{typeof(T).Name} 调用失败。传输通道: {channel}\n" + e);
                    client.Disconnect();
                }
            };
        }

        /// <summary>
        /// 注册网络消息委托
        /// </summary>
        /// <param name="action">传入网络消息</param>
        /// <typeparam name="T">网络消息</typeparam>
        /// <returns>返回一个消息委托</returns>
        internal static MessageDelegate GetMessage<T>(Action<T> action) where T : struct, Message
        {
            return (client, reader, channel) =>
            {
                try
                {
                    var message = reader.Invoke<T>();
                    action?.Invoke(message);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{typeof(T).Name} 调用失败。传输通道: {channel}\n" + e);
                    client.Disconnect();
                }
            };
        }

        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public static void CompressVarUInt(NetworkWriter writer, ulong value)
        {
            if (value <= 240)
            {
                writer.WriteByte((byte)value);
                return;
            }

            if (value <= 2287)
            {
                writer.WriteByte((byte)(((value - 240) >> 8) + 241));
                writer.WriteByte((byte)((value - 240) & 0xFF));
                return;
            }

            if (value <= 67823)
            {
                writer.WriteByte(249);
                writer.WriteByte((byte)((value - 2288) >> 8));
                writer.WriteByte((byte)((value - 2288) & 0xFF));
                return;
            }

            if (value <= 16777215)
            {
                writer.WriteByte(250);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                return;
            }

            if (value <= 4294967295)
            {
                writer.WriteByte(251);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                writer.WriteByte((byte)((value >> 24) & 0xFF));
                return;
            }

            if (value <= 1099511627775)
            {
                writer.WriteByte(252);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                writer.WriteByte((byte)((value >> 24) & 0xFF));
                writer.WriteByte((byte)((value >> 32) & 0xFF));
                return;
            }

            if (value <= 281474976710655)
            {
                writer.WriteByte(253);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                writer.WriteByte((byte)((value >> 24) & 0xFF));
                writer.WriteByte((byte)((value >> 32) & 0xFF));
                writer.WriteByte((byte)((value >> 40) & 0xFF));
                return;
            }

            if (value <= 72057594037927935)
            {
                writer.WriteByte(254);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                writer.WriteByte((byte)((value >> 24) & 0xFF));
                writer.WriteByte((byte)((value >> 32) & 0xFF));
                writer.WriteByte((byte)((value >> 40) & 0xFF));
                writer.WriteByte((byte)((value >> 48) & 0xFF));
            }
            else
            {
                writer.WriteByte(255);
                writer.WriteByte((byte)(value & 0xFF));
                writer.WriteByte((byte)((value >> 8) & 0xFF));
                writer.WriteByte((byte)((value >> 16) & 0xFF));
                writer.WriteByte((byte)((value >> 24) & 0xFF));
                writer.WriteByte((byte)((value >> 32) & 0xFF));
                writer.WriteByte((byte)((value >> 40) & 0xFF));
                writer.WriteByte((byte)((value >> 48) & 0xFF));
                writer.WriteByte((byte)((value >> 56) & 0xFF));
            }
        }

        /// <summary>
        /// 解压
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ulong DecompressVarUInt(NetworkReader reader)
        {
            byte a0 = reader.ReadByte();
            if (a0 < 241)
            {
                return a0;
            }

            byte a1 = reader.ReadByte();
            if (a0 <= 248)
            {
                return 240 + ((a0 - (ulong)241) << 8) + a1;
            }

            byte a2 = reader.ReadByte();
            if (a0 == 249)
            {
                return 2288 + ((ulong)a1 << 8) + a2;
            }

            byte a3 = reader.ReadByte();
            if (a0 == 250)
            {
                return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16);
            }

            byte a4 = reader.ReadByte();
            if (a0 == 251)
            {
                return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16) + ((ulong)a4 << 24);
            }

            byte a5 = reader.ReadByte();
            if (a0 == 252)
            {
                return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16) + ((ulong)a4 << 24) + ((ulong)a5 << 32);
            }

            byte a6 = reader.ReadByte();
            if (a0 == 253)
            {
                return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16) + ((ulong)a4 << 24) + ((ulong)a5 << 32) + ((ulong)a6 << 40);
            }

            byte a7 = reader.ReadByte();
            if (a0 == 254)
            {
                return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16) + ((ulong)a4 << 24) + ((ulong)a5 << 32) + ((ulong)a6 << 40) + ((ulong)a7 << 48);
            }

            byte a8 = reader.ReadByte();
            return a1 + ((ulong)a2 << 8) + ((ulong)a3 << 16) + ((ulong)a4 << 24) + ((ulong)a5 << 32) + ((ulong)a6 << 40) + ((ulong)a7 << 48) + ((ulong)a8 << 56);
        }
    }
}