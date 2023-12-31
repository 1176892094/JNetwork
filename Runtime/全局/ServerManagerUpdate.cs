using UnityEngine;

namespace JFramework.Net
{
    public partial class NetworkManager
    {
        public partial class ServerManager
        {
            /// <summary>
            /// 在Update之前调用
            /// </summary>
            internal void EarlyUpdate()
            {
                if (Transport.current != null)
                {
                    Transport.current.ServerEarlyUpdate();
                }

                foreach (var client in clients.Values)
                {
                    client.UpdateInterpolation();
                }
            }

            /// <summary>
            /// 在Update之后调用
            /// </summary>
            internal void AfterUpdate()
            {
                if (isActive)
                {
                    if (NetworkUtils.HeartBeat(Time.localTime, Instance.sendRate, ref sendTime))
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
            private void Broadcast()
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

                    client.OnUpdate();
                }
            }

            /// <summary>
            /// 被广播的指定客户端
            /// </summary>
            /// <param name="client">指定的客户端</param>
            private void BroadcastToClient(NetworkClient client)
            {
                foreach (var @object in spawns.Values)
                {
                    if (@object != null)
                    {
                        NetworkWriter writer = SerializeForClient(@object, client);
                        if (writer != null)
                        {
                            client.Send(new EntityMessage(@object.objectId, writer.ToArraySegment()));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"在观察列表中为 {client.clientId} 找到了空对象。请用NetworkServer.Destroy");
                    }
                }
            }

            /// <summary>
            /// 为客户端序列化 SyncVar
            /// </summary>
            /// <param name="object"></param>
            /// <param name="client"></param>
            /// <returns></returns>
            private NetworkWriter SerializeForClient(NetworkObject @object, NetworkClient client)
            {
                var serialize = @object.ServerSerializeTick(UnityEngine.Time.frameCount);

                if (@object.connection == client)
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
}