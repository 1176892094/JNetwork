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
        private static Smooth smooth = new Smooth(NetworkConst.PingWindow);

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
        public static void Update()
        {
            if (localTime - lastSendTime >= NetworkConst.PingInterval)
            {
                PingMessage message = new PingMessage(localTime); // 传入客户端时间到服务器
                NetworkClient.Send(message, Channel.Unreliable);
                lastSendTime = localTime;
            }
        }

        /// <summary>
        /// 服务器发送Pong消息给指定客户端
        /// </summary>
        public static void OnPingByServer(ClientEntity client, PingMessage message)
        {
            PongMessage pongMessage = new PongMessage(message.clientTime); //服务器将客户端时间传回到客户端
            client.Send(pongMessage, Channel.Unreliable);
        }

        /// <summary>
        /// 客户端从服务器接收的回传信息
        /// </summary>
        /// <param name="message"></param>
        public static void OnPongEvent(PongMessage message)
        {
            //TODO:进行平滑计算
            //smooth.Calculate( localTime - @event.clientTime);
        }

        /// <summary>
        /// 重置发送时间
        /// </summary>
        public static void ResetStatic()
        {
            lastSendTime = 0;
            smooth = new Smooth(NetworkConst.PingWindow);
        }
    }
}