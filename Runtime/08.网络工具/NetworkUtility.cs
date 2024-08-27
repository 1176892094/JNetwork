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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using UnityEngine;

namespace JFramework.Net
{
    using MessageDelegate = Action<NetworkClient, NetworkReader, byte>;

    public static class NetworkUtility
    {
        /// <summary>
        /// 根据名称获取Hash码
        /// </summary>
        /// <param name="name">传入名称</param>
        /// <returns>返回Hash码</returns>
        public static uint GetStableId(string name)
        {
            return unchecked(name.Aggregate(23U, (i, c) => i * 31 + c));
        }

        /// <summary>
        /// 获取随机值
        /// </summary>
        /// <returns></returns>
        internal static uint GetRandomId()
        {
            var cryptoRandomBuffer = new byte[4];
            RandomNumberGenerator.Fill(cryptoRandomBuffer);
            return MemoryMarshal.Read<uint>(cryptoRandomBuffer);
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
        /// 是否为场景网络对象
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        internal static bool IsSceneObject(NetworkObject @object)
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
        /// 注册网络消息委托
        /// </summary>
        /// <param name="action">传入网络连接，网络消息，传输通道</param>
        /// <typeparam name="T">网络消息</typeparam>
        /// <returns>返回一个消息委托</returns>
        internal static MessageDelegate GetMessage<T>(Action<NetworkClient, T, byte> action) where T : struct, Message
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
    }
}