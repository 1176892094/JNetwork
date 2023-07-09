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
        }
    }
}