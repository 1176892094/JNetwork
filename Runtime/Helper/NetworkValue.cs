using System;

namespace JFramework.Net
{
    public readonly struct NetworkValue : IEquatable<NetworkValue>
    {
        /// <summary>
        /// 网络对象Id
        /// </summary>
        public readonly uint objectId;
        
        /// <summary>
        /// 序列Id
        /// </summary>
        public readonly byte serialId;

        /// <summary>
        /// 构造函数初始化
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="serialId"></param>
        public NetworkValue(uint objectId, int serialId) : this()
        {
            this.objectId = objectId;
            this.serialId = (byte)serialId;
        }

        /// <summary>
        /// 比较自身和另一个相同的结构体
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(NetworkValue other)
        {
            return other.objectId == objectId && other.serialId == serialId;
        }

        /// <summary>
        /// 比较网络对象Id和序列Id
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="serialId"></param>
        /// <returns></returns>
        public bool Equals(uint objectId, int serialId)
        {
            return this.objectId == objectId && this.serialId == serialId;
        }
    }
}