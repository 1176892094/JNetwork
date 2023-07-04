using System.Runtime.CompilerServices;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    internal static class NetworkTime
    {
        private static float PingFrequency = 2;
        private static int PingWindowSize = 6;
        private static double lastPingTime;
        public static double localTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Time.unscaledTimeAsDouble;
        }
        
        internal static void UpdateClient()
        {
            if (localTime - lastPingTime >= PingFrequency)
            {
                NetworkPingMessage pingMessage = new NetworkPingMessage(localTime);
                NetworkClient.Send(pingMessage, Channel.Unreliable);
                lastPingTime = localTime;
            }
        }
        
        public static void RuntimeInitializeOnLoad()
        {
            PingFrequency = 2;
            PingWindowSize = 6;
            lastPingTime = 0;
        }
    }
}