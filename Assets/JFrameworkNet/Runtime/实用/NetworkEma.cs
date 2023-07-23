using System;

namespace JFramework.Net
{
    /// <summary>
    /// 网络移动指数平均值
    /// </summary>
    internal struct NetworkEma
    {
        /// <summary>
        /// 平滑因子
        /// </summary>
        private readonly double alpha;
        
        /// <summary>
        /// 是否初始化
        /// </summary>
        private bool start;
        
        /// <summary>
        /// 偏差
        /// </summary>
        private double variance;
        
        /// <summary>
        /// 数值
        /// </summary>
        public double value;
        
        /// <summary>
        /// 标准差
        /// </summary>
        public double deviation;

        /// <summary>
        /// 初始化平滑因子
        /// </summary>
        /// <param name="n"></param>
        public NetworkEma(int n)
        {
            alpha = 2.0 / (n + 1);
            start = false;
            value = 0;
            variance = 0;
            deviation = 0;
        }

        /// <summary>
        /// 计算平滑参数
        /// </summary>
        /// <param name="newValue"></param>
        public void Add(double newValue)
        {
            if (start)
            {
                double delta = newValue - value;
                value += alpha * delta;
                variance = (1 - alpha) * (variance + alpha * delta * delta);
                deviation = Math.Sqrt(variance);
            }
            else
            {
                value = newValue;
                start = true;
            }
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            start = false;
            value = 0;
            variance = 0;
            deviation = 0;
        }
    }
}