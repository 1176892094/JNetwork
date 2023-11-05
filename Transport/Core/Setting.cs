namespace JFramework.Udp
{
    public readonly struct Setting
    {
        public readonly int sendBufferSize;
        public readonly int receiveBufferSize;
        public readonly int maxTransferUnit;
        public readonly int resend;
        public readonly int timeout;
        public readonly uint receivePacketSize;
        public readonly uint sendPacketSize;
        public readonly uint interval;
        public readonly bool noDelay;
        public readonly bool congestion;

        public Setting
        (
            int sendBufferSize = 1024 * 1024 * 7,
            int receiveBufferSize = 1024 * 1024 * 7,
            int maxTransferUnit = Protocol.MTU_DEF,
            int timeout = Protocol.TIME_OUT,
            uint receivePacketSize = Protocol.WIN_RCV,
            uint sendPacketSize = Protocol.WIN_SND,
            uint interval = Protocol.INTERVAL,
            int resend = 0,
            bool noDelay = true,
            bool congestion = false)
        {
            this.receivePacketSize = receivePacketSize;
            this.sendPacketSize = sendPacketSize;
            this.timeout = timeout;
            this.maxTransferUnit = maxTransferUnit;
            this.sendBufferSize = sendBufferSize;
            this.receiveBufferSize = receiveBufferSize;
            this.interval = interval;
            this.resend = resend;
            this.noDelay = noDelay;
            this.congestion = congestion;
        }
    }
}