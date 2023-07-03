namespace Transport
{
    public readonly struct Setting
    {
        public readonly int sendBufferSize;
        public readonly int receiveBufferSize;
        public readonly int maxTransferUnit;
        public readonly int receivePacketSize;
        public readonly int sendPacketSize;
        public readonly int timeout;

        public Setting
        (
            int sendBufferSize = 1024 * 1024 * 7,
            int receiveBufferSize = 1024 * 1024 * 7,
            int maxTransferUnit = Jdp.MTU_DEF,
            int timeout = Jdp.TIME_OUT,
            int receivePacketSize = Jdp.WIN_RCV,
            int sendPacketSize = Jdp. WIN_SND)
        {
            this.receivePacketSize = receivePacketSize;
            this.sendPacketSize = sendPacketSize;
            this.timeout = timeout;
            this.maxTransferUnit = maxTransferUnit;
            this.sendBufferSize = sendBufferSize;
            this.receiveBufferSize = receiveBufferSize;
        }
    }
}