// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  14:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;

namespace JFramework.Net
{
    [Serializable]
    public struct NetworkVariable : IEquatable<NetworkVariable>
    {
        /// <summary>
        /// 网络对象Id
        /// </summary>
        public uint objectId;

        /// <summary>
        /// 序列Id
        /// </summary>
        public byte componentId;

        /// <summary>
        /// 构造函数初始化
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="componentId"></param>
        public NetworkVariable(uint objectId, int componentId)
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