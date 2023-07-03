using System;

namespace JFramework.Net
{
    public sealed partial class NetworkManager
    {
        public sealed class NetworkClient
        {
            public bool isActive;
            public Action OnConnected;
            public Action OnDisconnected;
            
            public void EarlyUpdate()
            {
            }

            public void AfterUpdate()
            {
            }
        }
    }
}