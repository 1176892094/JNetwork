using System;

namespace JFramework.Net
{
    /// <summary>
    /// 网络移动指数平均值
    /// </summary>
    public struct NetworkEma
    {
        /// <summary>
        /// 平滑因子
        /// </summary>
        private readonly double alpha;
        
        /// <summary>
        /// 是否初始化
        /// </summary>
        private bool initialized;
        
        /// <summary>
        /// 偏差
        /// </summary>
        private double Variance;
        
        /// <summary>
        /// 数值
        /// </summary>
        public double Value;
        
        /// <summary>
        /// 标准差
        /// </summary>
        public double StandardDeviation;

        /// <summary>
        /// 初始化平滑因子
        /// </summary>
        /// <param name="n"></param>
        public NetworkEma(int n)
        {
            alpha = 2.0 / (n + 1);
            initialized = false;
            Value = 0;
            Variance = 0;
            StandardDeviation = 0;
        }

        /// <summary>
        /// 计算平滑参数
        /// </summary>
        /// <param name="newValue"></param>
        public void Add(double newValue)
        {
            if (initialized)
            {
                double delta = newValue - Value;
                Value += alpha * delta;
                Variance = (1 - alpha) * (Variance + alpha * delta * delta);
                StandardDeviation = Math.Sqrt(Variance);
            }
            else
            {
                Value = newValue;
                initialized = true;
            }
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            initialized = false;
            Value = 0;
            Variance = 0;
            StandardDeviation = 0;
        }
    }
}