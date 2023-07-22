namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        /// <summary>
        /// 在Update前调用
        /// </summary>
        internal static void EarlyUpdate()
        {
            if (Transport.current != null)
            {
                Transport.current.ClientEarlyUpdate();
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
        
            if (server != null)
            {
                if (NetworkManager.mode == NetworkMode.Host)
                {
                    server.Update();
                }
                else
                {
                    if (isActive && isConnect)
                    {
                        NetworkTime.Update();
                        server.Update();
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
        private static void Broadcast()
        {
            if (!server.isReady) return;
            if (NetworkServer.isActive) return;
            foreach (var @object in spawns.Values)
            {
                using var writer = NetworkWriter.Pop();
                @object.ClientSerialize(writer);
                if (writer.position > 0)
                {
                    Send(new EntityEvent(@object.objectId, writer.ToArraySegment()));
                    @object.ClearDirty();
                }
            }

            //Send(new TimeEvent(), Channel.Unreliable);
        }
    }
}