using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkUtils
    {
        /// <summary>
        /// 是场景物体
        /// </summary>
        public static bool IsSceneObject(NetworkObject entity)
        {
            var gameObject = entity.gameObject;
            if (entity.sceneId == 0) return false;
            return gameObject.hideFlags is not (HideFlags.HideAndDontSave or HideFlags.NotEditable);
        }

        /// <summary>
        /// 拥有有效的父物体
        /// </summary>
        public static bool IsValidParent(NetworkObject entity)
        {
            var parent = entity.transform.parent;
            return parent == null || parent.gameObject.activeInHierarchy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkObject GetNetworkIdentity(uint netId)
        {
            // if (NetworkServer.isActive)
            // {
            //     NetworkServer.TryGetNetId(netId, out var identity);
            //     return identity;
            // }
            //
            // if (NetworkClient.isActive)
            // {
            //     NetworkClient.TryGetNetId(netId, out var identity);
            //     return identity;
            // }

            return null;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Elapsed(double current, double interval, ref double lastTime)
        {
            if (current < lastTime + interval) return false;
            var clientTime = (long)(current / interval);
            lastTime = clientTime * interval;
            return true;
        }

        /// <summary>
        /// 写入消息Id
        /// </summary>
        /// <param name="message"></param>
        /// <param name="writer"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteMessage<T>(NetworkWriter writer, T message) where T : struct, IEvent
        {
            writer.WriteUShort(MessageId<T>.Id);
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

        internal static MessageDelegate Register<T1, T2>(Action<T1, T2, Channel> handle, bool isAuthority) where T1 : Connection where T2 : struct, IEvent
        {
            return (connection, reader, channel) =>
            {
                try
                {
                    if (isAuthority && !connection.isAuthority)
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
                    Debug.LogError($"Disconnected clientId {((ClientObject)connection).clientId}\n{e}");
                    connection.Disconnect();
                }
            };
        }

        internal static MessageDelegate Register<T1, T2>(Action<T1, T2> handle, bool isAuthority) where T1 : Connection where T2 : struct, IEvent
        {
            return Register((Action<T1, T2, Channel>)Wrapped, isAuthority);

            void Wrapped(T1 connection, T2 reader, Channel channel)
            {
                handle?.Invoke(connection, reader);
            }
        }

        internal static MessageDelegate Register<T1>(Action<T1> handle, bool isAuthority) where T1 : struct, IEvent
        {
            return Register((Action<Connection, T1>)Wrapped, isAuthority);

            void Wrapped(Connection connection, T1 reader)
            {
                handle?.Invoke(reader);
            }
        }

        /// <summary>
        /// 根据名称获取Hash码
        /// </summary>
        /// <param name="name">传入名称</param>
        /// <returns>返回Hash码</returns>
        public static int GetHashByName(string name)
        {
            unchecked
            {
                return name.Aggregate(23, (hash, c) => hash * 31 + c);
            }
        }
    }

    public static class MessageId<T> where T : struct, IEvent
    {
        public static readonly ushort Id = (ushort)NetworkUtils.GetHashByName(typeof(T).FullName);
    }
}