using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    internal sealed class SettingManager : Component<NetworkManager>
    {
        [InfoBox("本地模拟相对于发送间隔 * 缓冲时间乘数 的滞后时间")]
        public double bufferTimeMultiplier = 2;

        [InfoBox("当本地时间线快速朝向远程时间时，减速开始")]
        public float catchupNegativeThreshold = -1;
        
        [InfoBox("当本地时间线移动太慢，距离远程时间太远时，开始追赶")]
        public float catchupPositiveThreshold = 1;
        
        [InfoBox("在追赶时本地时间线的加速百分比")]
        [Range(0, 1)] public double catchupSpeed = 0.02f;
        
        [InfoBox("在减速时本地时间线的减速百分比")]
        [Range(0, 1)] public double slowdownSpeed = 0.04f;
        
        [InfoBox("追赶/减速通过 n 秒的指数移动平均调整")]
        public int driftEmaDuration = 1;
        
        [InfoBox("自动调整 bufferTimeMultiplier 以获得平滑结果")]
        public bool dynamicAdjustment = true;
        
        [InfoBox("动态调整时始终添加到 bufferTimeMultiplier 的安全缓冲")]
        public float dynamicAdjustmentTolerance = 1;
        
        [InfoBox("动态调整通过 n 秒的指数移动平均标准差计算")]
        public int deliveryTimeEmaDuration = 2;
    }
}