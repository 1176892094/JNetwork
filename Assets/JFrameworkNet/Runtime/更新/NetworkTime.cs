using System.Runtime.CompilerServices;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    internal static class NetworkTime
    {
        private static float PingFrequency = 2;
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
            if (localTime - lastPingTime >= PingFrequency)
            {
                PingEvent pingEvent = new PingEvent()
                {
                    clientTime = localTime,
                };
                ClientManager.Send(pingEvent, Channel.Unreliable);
                lastPingTime = localTime;
            }
        }

        internal static void OnClientPong()
        {
        }

        public static void RuntimeInitializeOnLoad()
        {
            PingFrequency = 2;
            lastPingTime = 0;
        }
    }
}