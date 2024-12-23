using System;

namespace JFramework.Net
{
    [Serializable]
    public struct NetworkVariable : IEquatable<NetworkVariable>
    {
        public uint objectId;
        public byte componentId;
        
        public NetworkVariable(uint objectId, int componentId)
        {
            this.objectId = objectId;
            this.componentId = (byte)componentId;
        }
        
        public bool Equals(uint objectId, int componentId)
        {
            return this.objectId == objectId && this.componentId == componentId;
        }
        
        public bool Equals(NetworkVariable other)
        {
            return objectId == other.objectId && componentId == other.componentId;
        }

        public override bool Equals(object obj)
        {
            return obj is NetworkVariable other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(objectId, componentId);
        }
    }
}