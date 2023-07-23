using System;

namespace JFramework.Net
{
    internal struct Smooth
    {
        /// <summary>
        /// 是否初始化
        /// </summary>
        private bool start;
        
        /// <summary>
        /// 数值
        /// </summary>
        public double value;
        
        /// <summary>
        /// 标准差
        /// </summary>
        public double deviation;
        
        /// <summary>
        /// 偏差
        /// </summary>
        private double variance;
        
        /// <summary>
        /// 平滑因子
        /// </summary>
        private readonly double alpha;

        /// <summary>
        /// 初始化平滑因子
        /// </summary>
        /// <param name="size"></param>
        public Smooth(int size)
        {
            start = false;
            value = 0;
            variance = 0;
            deviation = 0;
            alpha = 2.0 / (size + 1);
        }

        /// <summary>
        /// 计算平滑参数
        /// </summary>
        /// <param name="newValue"></param>
        public void Calculate(double newValue)
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