using System;

namespace JFramework.Net
{
    public readonly struct NetworkValue : IEquatable<NetworkValue>
    {
        public readonly byte index;
        public readonly uint netId;

        public NetworkValue(uint netId, int index) : this()
        {
            this.netId = netId;
            this.index = (byte)index;
        }

        public bool Equals(NetworkValue other)
        {
            return other.netId == netId && other.index == index;
        }

        public bool Equals(uint netId, int index)
        {
            return this.netId == netId && this.index == index;
        }

        public override string ToString()
        {
            return $"[NetworkIndex : {netId} ComponentIndex : {index}]";
        }
    }
}