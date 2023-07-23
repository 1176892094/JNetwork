using UnityEngine;

namespace JFramework.Net
{
    public class SnapshotData
    {
        /// <summary>
        /// 本地模拟相对于发送间隔 * 缓冲时间乘数 的滞后时间
        /// </summary>
        public double bufferTimeMultiplier = 2;
        
        /// <summary>
        /// 当本地时间线快速朝向远程时间时，减速开始
        /// </summary>
        public float catchupNegativeThreshold = -1;
        
        /// <summary>
        /// 当本地时间线移动太慢，距离远程时间太远时，开始追赶
        /// </summary>
        public float catchupPositiveThreshold = 1;
        
        /// <summary>
        /// 在追赶时本地时间线的加速百分比
        /// </summary>
        [Range(0, 1)] public double catchupSpeed = 0.02f;
        
        /// <summary>
        /// 在减速时本地时间线的减速百分比
        /// </summary>
        [Range(0, 1)] public double slowdownSpeed = 0.04f;
        
        /// <summary>
        /// 追赶/减速通过 n 秒的指数移动平均调整
        /// </summary>
        public int driftEmaDuration = 1;
        
        /// <summary>
        /// 自动调整 bufferTimeMultiplier 以获得平滑结果
        /// </summary>
        public bool dynamicAdjustment = true;

        /// <summary>
        /// 动态调整时始终添加到 bufferTimeMultiplier 的安全缓冲
        /// </summary>
        public float dynamicAdjustmentTolerance = 1;
        
        /// <summary>
        /// 动态调整通过 n 秒的指数移动平均标准差计算
        /// </summary>
        public int deliveryTimeEmaDuration = 2;
    }
}