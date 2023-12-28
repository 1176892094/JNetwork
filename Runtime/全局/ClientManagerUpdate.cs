namespace JFramework.Net
{
    public partial class NetworkManager
    {
        public partial class ClientManager
        {
            /// <summary>
            /// 在Update前调用
            /// </summary>
            internal void EarlyUpdate()
            {
                if (Transport.current != null)
                {
                    Transport.current.ClientEarlyUpdate();
                }

                connection?.UpdateInterpolation();
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

                if (connection != null)
                {
                    if (Instance.mode == NetworkMode.Host)
                    {
                        connection.OnUpdate();
                    }
                    else
                    {
                        if (isActive && isAuthority)
                        {
                            Time.OnUpdate();
                            connection.OnUpdate();
                        }
                    }
                }

                if (Transport.current != null)
                {
                    Transport.current.ClientAfterUpdate();
                }
            }

            /// <summary>
            /// 客户端进行广播
            /// </summary>
            private void Broadcast()
            {
                if (!connection.isReady) return;
                if (Server.isActive) return;
                foreach (var @object in spawns.Values)
                {
                    using var writer = NetworkWriter.Pop();
                    @object.ClientSerialize(writer);
                    if (writer.position > 0)
                    {
                        Send(new EntityMessage(@object.objectId, writer.ToArraySegment()));
                        @object.ClearDirty();
                    }
                }

                Send(new SnapshotMessage(), Channel.Unreliable);
            }
        }
    }
}