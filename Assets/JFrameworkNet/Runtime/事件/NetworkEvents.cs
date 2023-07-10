using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JFramework.Interface;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 静态泛型事件Id
    /// </summary>
    /// <typeparam name="T">网络事件类型</typeparam>
    public static class EventId<T> where T : struct, IEvent
    {
        /// <summary>
        /// 根据泛型类型的名称来获取Hash的Id
        /// </summary>
        public static readonly ushort Id = (ushort)NetworkEvent.GetIdByName(typeof(T).FullName);
    }
    
    public static class NetworkEvent
    {
        /// <summary>
        /// 根据名称获取Hash码
        /// </summary>
        /// <param name="name">传入名称</param>
        /// <returns>返回Hash码</returns>
        public static int GetIdByName(string name)
        {
            unchecked
            {
                return name.Aggregate(23, (hash, c) => hash * 31 + c);
            }
        }
        
        /// <summary>
        /// 写入事件Id
        /// </summary>
        /// <param name="event"></param>
        /// <param name="writer"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteEvent<T>(NetworkWriter writer, T @event) where T : struct, IEvent
        {
            writer.WriteUShort(EventId<T>.Id);
            writer.Write(@event);
        }

        /// <summary>
        /// 读取消息Id
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadEvent(NetworkReader reader, out ushort eventId)
        {
            try
            {
                eventId = reader.ReadUShort();
                return true;
            }
            catch (EndOfStreamException)
            {
                eventId = 0;
                return false;
            }
        }

        /// <summary>
        /// 注册网络消息委托
        /// </summary>
        /// <param name="handle">传入网络连接，网络消息，传输通道</param>
        /// <param name="authority">是否需要认证权限</param>
        /// <typeparam name="T1">网络连接(Server or Client)</typeparam>
        /// <typeparam name="T2">网络消息</typeparam>
        /// <returns>返回一个消息委托</returns>
        internal static EventDelegate Register<T1, T2>(Action<T1, T2, Channel> handle, bool authority) where T1 : Connection where T2 : struct, IEvent
        {
            return (connection, reader, channel) =>
            {
                try
                {
                    if (authority && !connection.isAuthority)
                    {
                        Debug.LogWarning($"Send message no authority: {connection}");
                        connection.Disconnect();
                        return;
                    }

                    var message = reader.Read<T2>();
                    handle?.Invoke((T1)connection, message, channel);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Disconnected clientId {((ClientEntity)connection).clientId}\n{e}");
                    connection.Disconnect();
                }
            };
        }

        /// <summary>
        /// 注册网络消息委托
        /// </summary>
        /// <param name="handle">传入网络连接，网络消息</param>
        /// <param name="authority">是否需要认证权限</param>
        /// <typeparam name="T1">网络连接(Server or Client)</typeparam>
        /// <typeparam name="T2">网络消息</typeparam>
        /// <returns>返回一个消息委托</returns>
        internal static EventDelegate Register<T1, T2>(Action<T1, T2> handle, bool authority) where T1 : Connection where T2 : struct, IEvent
        {
            return Register((Action<T1, T2, Channel>)Wrapped, authority);

            void Wrapped(T1 connection, T2 reader, Channel channel)
            {
                handle?.Invoke(connection, reader);
            }
        }

        /// <summary>
        /// 注册网络消息委托
        /// </summary>
        /// <param name="handle">传入网络消息</param>
        /// <param name="authority">是否需要认证权限</param>
        /// <typeparam name="T1">网络消息</typeparam>
        /// <returns>返回一个消息委托</returns>
        internal static EventDelegate Register<T1>(Action<T1> handle, bool authority) where T1 : struct, IEvent
        {
            return Register((Action<Connection, T1>)Wrapped, authority);

            void Wrapped(Connection connection, T1 reader)
            {
                handle?.Invoke(reader);
            }
        }
    }
}