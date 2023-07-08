using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        public static void EarlyUpdate()
        {
            if (Transport.current != null)
            {
                Transport.current.ClientEarlyUpdate();
            }
        }

        public static void AfterUpdate()
        {
            if (isActive)
            {
                if (NetworkUtils.Elapsed(NetworkTime.localTime, sendRate, ref lastSendTime))
                {
                    Broadcast();
                }
            }
        
            if (connection != null)
            {
                if (connection.isLocal)
                {
                    connection.Update();
                }
                else
                {
                    if (isActive && isConnect)
                    {
                        NetworkTime.UpdateClient();
                        connection.Update();
                    }
                }
            }
            
            if (Transport.current != null)
            {
                Transport.current.ClientAfterUpdate();
            }
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
        
        private static void BroadcastTimeSnapshot()
        {
            Send(new SnapshotMessage(), Channel.Unreliable);
        }
    }
}