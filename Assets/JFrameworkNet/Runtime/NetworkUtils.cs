using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkUtils
    {
        /// <summary>
        /// 是场景物体
        /// </summary>
        public static bool IsSceneObject(NetworkIdentity entity)
        {
            var gameObject = entity.gameObject;
            if (entity.sceneId == 0) return false;
            return gameObject.hideFlags is not (HideFlags.HideAndDontSave or HideFlags.NotEditable);
        }

        /// <summary>
        /// 拥有有效的父物体
        /// </summary>
        public static bool IsValidParent(NetworkIdentity entity)
        {
            var parent = entity.transform.parent;
            return parent == null || parent.gameObject.activeInHierarchy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkIdentity GetNetworkIdentity(uint netId)
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

        /// <summary>
        /// 写入消息Id
        /// </summary>
        /// <param name="message"></param>
        /// <param name="writer"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteMessage<T>(NetworkWriter writer, T message) where T : struct, NetworkMessage
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

    public static class MessageId<T> where T : struct, NetworkMessage
    {
        public static readonly ushort Id = (ushort)NetworkUtils.GetHashByName(typeof(T).FullName);
    }
}