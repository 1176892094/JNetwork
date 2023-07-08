using System.Runtime.CompilerServices;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    internal static class NetworkTime
    {
        private static float PingFrequency = 2;
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
                PingMessage pingMessage = new PingMessage(localTime);
                NetworkClient.Send(pingMessage, Channel.Unreliable);
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