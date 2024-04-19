using System;

namespace JFramework.Net
{
    public readonly struct NetworkVariable : IEquatable<NetworkVariable>
    {
        /// <summary>
        /// 网络对象Id
        /// </summary>
        public readonly uint objectId;
        
        /// <summary>
        /// 序列Id
        /// </summary>
        public readonly byte componentId;

        /// <summary>
        /// 构造函数初始化
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="componentId"></param>
        public NetworkVariable(uint objectId, int componentId) : this()
        {
            this.objectId = objectId;
            this.componentId = (byte)componentId;
        }

        /// <summary>
        /// 比较自身和另一个相同的结构体
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(NetworkVariable other)
        {
            return other.objectId == objectId && other.componentId == componentId;
        }

        /// <summary>
        /// 比较网络对象Id和序列Id
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="componentId"></param>
        /// <returns></returns>
        public bool Equals(uint objectId, int componentId)
        {
            return this.objectId == objectId && this.componentId == componentId;
        }
    }
}