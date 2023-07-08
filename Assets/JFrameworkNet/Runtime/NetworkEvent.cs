using System;
using System.Collections.Generic;
using JFramework.Udp;

namespace JFramework.Net
{
    internal static class NetworkEvent
    {
        private static readonly Dictionary<ushort, MessageDelegate> clientEvents = new Dictionary<ushort, MessageDelegate>();
        private static readonly Dictionary<ushort, MessageDelegate> serverEvents = new Dictionary<ushort, MessageDelegate>();
        
        /// <summary>
        /// 注册网络消息
        /// </summary>
        public static void RegisterMessage<T>(Action<T> handle, bool isAuthority = true) where T : struct, NetworkMessage
        {
            clientEvents[MessageId<T>.Id] = NetworkUtils.Register(handle, isAuthority);
        }
        
        /// <summary>
        /// 注册网络消息
        /// </summary>
        public static void RegisterMessage<T>(Action<ClientObject, T> handle, bool isAuthority = true) where T : struct, NetworkMessage
        {
            serverEvents[MessageId<T>.Id] = NetworkUtils.Register(handle, isAuthority);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        public static void RegisterMessage<T>(Action<ClientObject, T, Channel> handle, bool isAuthority = true) where T : struct, NetworkMessage
        {
            serverEvents[MessageId<T>.Id] = NetworkUtils.Register(handle, isAuthority);
        }

        /// <summary>
        /// 客户端消息事件
        /// </summary>
        public static bool ClientMessage(ushort key, ServerObject server, NetworkReader reader, Channel channel)
        {
            if (clientEvents.TryGetValue(key, out MessageDelegate handle))
            {
                handle.Invoke(server, reader, channel);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 服务器消息事件
        /// </summary>
        public static bool ServerMessage(ushort key, ClientObject client, NetworkReader reader, Channel channel)
        {
            if (serverEvents.TryGetValue(key, out MessageDelegate handle))
            {
                handle.Invoke(client, reader, channel);
                return true;
            }

            return false;
        }
        
        public static void RuntimeInitializeOnLoad()
        {
            clientEvents.Clear();
            serverEvents.Clear();
        }
    }
}