namespace JFramework.Net
{
    public static partial class ClientManager
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
                if (NetworkUtils.TimeTick(NetworkTime.localTime, sendRate, ref lastSendTime))
                {
                    Broadcast();
                }
            }
        
            if (connection != null)
            {
                if (NetworkManager.mode == NetworkMode.Host)
                {
                    connection.Update();
                }
                else
                {
                    if (isActive && isAuthority)
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

        /// <summary>
        /// 客户端进行广播
        /// </summary>
        private static void Broadcast()
        {
        }
    }
}