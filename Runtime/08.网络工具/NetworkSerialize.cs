// *********************************************************************************
// # Project: Forest
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-12-03  14:12
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

namespace JFramework.Net
{
    /// <summary>
    /// 网络变量序列化
    /// </summary>
    internal struct NetworkSerialize
    {
        /// <summary>
        /// 所在帧
        /// </summary>
        public int frameCount;

        /// <summary>
        /// 所有者模式
        /// </summary>
        public readonly NetworkWriter owner;

        /// <summary>
        /// 观察者模式
        /// </summary>
        public readonly NetworkWriter observer;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="frameCount">传入当前帧</param>
        public NetworkSerialize(int frameCount)
        {
            owner = new NetworkWriter();
            observer = new NetworkWriter();
            this.frameCount = frameCount;
        }
    }
}