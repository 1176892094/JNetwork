using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        internal static void EarlyUpdate()
        {
            if (Transport.current != null)
            {
                Transport.current.ServerEarlyUpdate();
            }
        }

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

        private static void Broadcast()
        {
            copies.Clear();
            copies.AddRange(clients.Values);
            foreach (var client in copies)
            {
                if (client.isReady)
                {
                    client.Send(new SnapshotMessage(), Channel.Unreliable);
                    BroadcastToClient(client);
                }

                client.Update();
            }
        }

        private static void BroadcastToClient(ClientConnection client)
        {
        }
    }
}