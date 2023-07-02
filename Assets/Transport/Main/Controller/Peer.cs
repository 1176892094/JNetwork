namespace Transport
{
    public class Peer
    {
        private readonly uint cookie;
        private readonly Setting setting;
        private readonly PeerData peerData;

        public Peer(PeerData peerData, Setting setting, uint cookie)
        {
            this.cookie = cookie;
            this.setting = setting;
            this.peerData = peerData;
        }
    }
}