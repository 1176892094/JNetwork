using JFramework.Udp;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        /// <summary>
        /// 在Update之前调用
        /// </summary>
        internal static void EarlyUpdate()
        {
            if (Transport.current != null)
            {
                Transport.current.ServerEarlyUpdate();
            }
        }

        /// <summary>
        /// 在Update之后调用
        /// </summary>
        internal static void AfterUpdate()
        {
            if (isActive)
            {
                if (NetworkUtils.Elapsed(NetworkTime.localTime, sendRate, ref lastSendTime))
                {
                    Broadcast();
                }
            }

            if (Transport.current != null)
            {
                Transport.current.ServerAfterUpdate();
            }
        }

        /// <summary>
        /// 服务器对所有客户端进行广播和更新
        /// </summary>
        private static void Broadcast()
        {
            copies.Clear();
            copies.AddRange(clients.Values);
            foreach (var client in copies)
            {
                if (client.isReady)
                {
                    client.Send(new SnapshotEvent(), Channel.Unreliable);
                    BroadcastToClient(client);
                }

                client.Update();
            }
        }

        /// <summary>
        /// 被广播的指定客户端
        /// </summary>
        /// <param name="client">指定的客户端</param>
        private static void BroadcastToClient(ClientEntity client)
        {
        }
    }
}