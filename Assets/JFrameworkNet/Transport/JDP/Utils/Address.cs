using System;

namespace JFramework.Udp
{
    [Serializable]
    public struct Address
    {
        public string ip;
        public ushort port;

        public Address(string ip, ushort port)
        {
            this.ip = ip;
            this.port = port;
        }
    }
}