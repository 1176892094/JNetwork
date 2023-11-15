using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 静态泛型消息Id
    /// </summary>
    /// <typeparam name="T">网络消息类型</typeparam>
    public static class NetworkMessage<T> where T : struct, Message
    {
        /// <summary>
        /// 根据泛型类型的名称来获取Hash的Id
        /// </summary>
        public static readonly ushort Id = (ushort)NetworkMessage.GetHashByName(typeof(T).FullName);
    }
    
    public static class NetworkMessage
    {
        /// <summary>
        /// 根据名称获取Hash码
        /// </summary>
        /// <param name="name">传入名称</param>
        /// <returns>返回Hash码</returns>
        public static uint GetHashByName(string name)
        {
            unchecked
            {
                return name.Aggregate(23U, (hash, c) => hash * 31 + c);
            }
        }
        
        /// <summary>
        /// 写入消息Id
        /// </summary>
        /// <param name="message"></param>
        /// <param name="writer"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteMessage<T>(NetworkWriter writer, T message) where T : struct, Message
        {
            writer.WriteUShort(NetworkMessage<T>.Id);
            writer.Write(message);
        }

        /// <summary>
        /// 读取消息Id
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadMessage(NetworkReader reader, out ushort messageId)
        {
            try
            {
                messageId = reader.ReadUShort();
                return true;
            }
            catch (EndOfStreamException)
            {
                messageId = 0;
                return false;
            }
        }

        /// <summary>
        /// 注册网络消息委托
        /// </summary>
        /// <param name="handle">传入网络连接，网络消息，传输通道</param>
        /// <typeparam name="T1">网络连接(Server or Client)</typeparam>
        /// <typeparam name="T2">网络消息</typeparam>
        /// <returns>返回一个消息委托</returns>
        internal static MessageDelegate Register<T1, T2>(Action<T1, T2, Channel> handle) where T1 : UnityPeer where T2 : struct, Message
        {
            return (connection, reader, channel) =>
            {
                try
                {
                    var message = reader.Read<T2>();
                    handle?.Invoke((T1)connection, message, channel);
                }
                catch (Exception e)
                {
                    if (connection is UnityClient client)
                    {
                        Debug.LogError($"断开连接。客户端：{client.clientId}\n{e}");
                    }
                    else
                    {
                        Debug.LogError(e.ToString());
                    }

                    connection?.Disconnect();
                }
            };
        }

        /// <summary>
        /// 注册网络消息委托
        /// </summary>
        /// <param name="handle">传入网络连接，网络消息</param>
        /// <typeparam name="T1">网络连接(Server or Client)</typeparam>
        /// <typeparam name="T2">网络消息</typeparam>
        /// <returns>返回一个消息委托</returns>
        internal static MessageDelegate Register<T1, T2>(Action<T1, T2> handle) where T1 : UnityPeer where T2 : struct, Message
        {
            return Register((Action<T1, T2, Channel>)Handle);

            void Handle(T1 connection, T2 @event, Channel channel)
            {
                handle?.Invoke(connection, @event);
            }
        }

        /// <summary>
        /// 注册网络消息委托
        /// </summary>
        /// <param name="handle">传入网络消息</param>
        /// <typeparam name="T1">网络消息</typeparam>
        /// <returns>返回一个消息委托</returns>
        internal static MessageDelegate Register<T1>(Action<T1> handle) where T1 : struct, Message
        {
            return Register((Action<UnityPeer, T1>)Handle);

            void Handle(UnityPeer connection, T1 @event)
            {
                handle?.Invoke(@event);
            }
        }
    }
}