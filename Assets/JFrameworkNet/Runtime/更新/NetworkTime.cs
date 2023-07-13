using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    internal static class NetworkTime
    {
        private static double lastPingTime;

        /// <summary>
        /// 当前网络时间
        /// </summary>
        public static double localTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Time.unscaledTimeAsDouble;
        }

        /// <summary>
        /// 客户端发送Ping消息到服务器端
        /// </summary>
        internal static void UpdateClient()
        {
            if (localTime - lastPingTime >= NetworkConst.Ping)
            {
                PingEvent pingEvent = new PingEvent()
                {
                    clientTime = localTime,
                };
                ClientManager.Send(pingEvent, Channel.Unreliable);
                lastPingTime = localTime;
            }
        }

        /// <summary>
        /// 服务器发送Pong消息给指定客户端
        /// </summary>
        internal static void OnPingEvent(ClientEntity client, PingEvent @event)
        {
            PongEvent pongEvent = new PongEvent
            {
                clientTime = @event.clientTime,
            };
            client.Send(pongEvent, Channel.Unreliable);
        }

        /// <summary>
        /// 客户端从服务器接收的Pong
        /// </summary>
        /// <param name="event"></param>
        internal static void OnPongEvent(PongEvent @event)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public static void RuntimeInitializeOnLoad()
        {
            lastPingTime = 0;
        }
    }
}