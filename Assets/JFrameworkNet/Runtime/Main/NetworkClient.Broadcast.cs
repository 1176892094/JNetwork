using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        private static void BroadcastTimeSnapshot()
        {
            Send(new SnapshotMessage(), Channel.Unreliable);
        }
        
        private static void Broadcast()
        {
            if (!connection.isReady) return;
            if (NetworkServer.isActive) return;
            BroadcastTimeSnapshot();
            
            foreach (var identity in connection.objects)
            {
                if (identity != null)
                {
                    // using var writer = NetworkWriterPool.Pop();
                    // identity.SerializeClient(writer);
                    // if (writer.position > 0)
                    // {
                    //     var message = new EntityStateMessage
                    //     {
                    //         netId = identity.netId,
                    //         payload = writer.ToArraySegment()
                    //     };
                    //         
                    //     Send(message);
                    //     identity.ClearDirtyComponentsDirtyBits();
                    // }
                }
                else
                {
                    Debug.LogWarning($"Found 'null' entry in owned list for client. This is unexpected behaviour.");
                }
            }
        }
    }
}