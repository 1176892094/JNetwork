using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
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
            foreach (var identity in client.observing)
            {
                if (identity != null)
                {
                    // var writer = SerializeForConnection(identity, client);
                    // if (writer != null)
                    // {
                    //     var message = new EntityMessage
                    //     {
                    //         netId = identity.netId,
                    //         segment = writer.ToArraySegment()
                    //     };
                    //     client.Send(message);
                    // }
                }
                else
                {
                    Debug.LogWarning($"The {identity} not entry in observing list for clientId = {client.clientId}");
                }
            }
        }
    }
}