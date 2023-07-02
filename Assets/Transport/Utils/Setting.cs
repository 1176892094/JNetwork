namespace Transport
{
    public readonly struct Setting
    {
        public readonly int sendBufferSize;
        public readonly int receiveBufferSize;
        public readonly int maxTransferUnit;
        public readonly int packageReceive;
        public readonly int packageSend;
        public readonly int timeout;

        public Setting
        (
            int sendBufferSize = 1024 * 1024 * 7,
            int receiveBufferSize = 1024 * 1024 * 7,
            int maxTransferUnit = Utils.MaxTransferUnit,
            int timeout = Utils.Timeout,
            int packageReceive = Utils.PackageReceive,
            int packageSend = Utils.PackageSend)
        {
            this.packageReceive = packageReceive;
            this.packageSend = packageSend;
            this.timeout = timeout;
            this.maxTransferUnit = maxTransferUnit;
            this.sendBufferSize = sendBufferSize;
            this.receiveBufferSize = receiveBufferSize;
        }
    }
}