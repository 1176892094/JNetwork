using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    internal static class NetworkTime
    {
        /// <summary>
        /// 上一次发送Ping的时间
        /// </summary>
        private static double lastSendTime;

        /// <summary>
        /// 客户端回传往返时间
        /// </summary>
        private static NetworkEma roundTripTime = new NetworkEma(NetworkConst.PingWindow);

        /// <summary>
        /// 当前网络时间
        /// </summary>
        public static double localTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Time.unscaledTimeAsDouble;
        }
        
        public static double fixedTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => NetworkManager.Server.isActive ? localTime : NetworkManager.Client.connection.localTimeline;
        }

        /// <summary>
        /// 客户端发送Ping消息到服务器端
        /// </summary>
        public static void Update()
        {
            if (localTime - lastSendTime >= NetworkConst.PingInterval)
            {
                PingMessage message = new PingMessage(localTime); // 传入客户端时间到服务器
                NetworkManager.Client.SendMessage(message, Channel.Unreliable);
                lastSendTime = localTime;
            }
        }

        /// <summary>
        /// 服务器发送Pong消息给指定客户端
        /// </summary>
        public static void OnPingByServer(UnityClient client, PingMessage message)
        {
            PongMessage pongMessage = new PongMessage(message.clientTime); //服务器将客户端时间传回到客户端
            client.SendMessage(pongMessage, Channel.Unreliable);
        }

        /// <summary>
        /// 客户端从服务器接收的回传信息
        /// </summary>
        /// <param name="message"></param>
        public static void OnPongByClient(PongMessage message)
        { 
            roundTripTime.Calculate(localTime - message.clientTime);
            NetworkManager.Instance.ClientPingUpdate(roundTripTime.value);
        }

        /// <summary>
        /// 重置发送时间
        /// </summary>
        public static void ResetStatic()
        {
            lastSendTime = 0;
            roundTripTime = new NetworkEma(NetworkConst.PingWindow);
        }
    }
}