using UnityEngine;

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
                if (NetworkUtils.TimeTick(NetworkTime.localTime, NetworkManager.sendRate, ref lastSendTime))
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
                   // client.Send(new TimeEvent(), Channel.Unreliable);
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
            foreach (var @object in connection.observers)
            {
                if (@object != null)
                {
                    NetworkWriter writer = SerializeForClient(@object, client);
                    if (writer != null)
                    {
                        client.Send(new EntityEvent(@object.objectId, writer.ToArraySegment()));
                    }
                }
                else
                {
                    Debug.LogWarning($"在观察列表中为 {client.clientId} 找到了空对象。请用NetworkServer.Destroy");
                }
            }
        }

        private static NetworkWriter SerializeForClient(NetworkObject @object, ClientEntity client)
        {
            var serialize = @object.GetServerSerializationAtTick(Time.frameCount);
            
            if (@object.client == client)
            {
                if (serialize.owner.position > 0)
                {
                    return serialize.owner;
                }
            }
            else
            {
                if (serialize.observer.position > 0)
                {
                    return serialize.observer;
                }
            }
            
            return null;
        }
    }
}