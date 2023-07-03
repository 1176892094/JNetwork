using System;

namespace JFramework.Net
{
    public readonly struct NetworkVariable : IEquatable<NetworkVariable>
    {
        public readonly byte index;
        public readonly uint netId;

        public NetworkVariable(uint netId, int index) : this()
        {
            this.netId = netId;
            this.index = (byte)index;
        }

        public bool Equals(NetworkVariable other)
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