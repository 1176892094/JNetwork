using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JFramework.Net
{
    internal static class SnapshotUtils
    {
        /// <summary>
        /// 安全区间 = (发送间隔+抖动标准偏差) / 发送间隔 + 动态调整公差
        /// </summary>
        public static double DynamicAdjust(double sendRate, double jitterStandardDeviation, double dynamicAdjustmentTolerance)
        {
            double intervalWithJitter = sendRate + jitterStandardDeviation;
            double multiples = intervalWithJitter / sendRate;
            double safeZone = multiples + dynamicAdjustmentTolerance;
            return safeZone;
        }
        
        /// <summary>
        /// 插入并调整快照顺序
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="snapshot"></param>
        /// <param name="localTimeline"></param>
        /// <param name="localTimescale"></param>
        /// <param name="sendRate"></param>
        /// <param name="bufferTime"></param>
        /// <param name="driftEma"></param>
        /// <param name="deliveryTimeEma"></param>
        /// <typeparam name="T"></typeparam>
        public static void InsertAndAdjust<T>(SortedList<double, T> buffer, T snapshot, ref double localTimeline, ref double localTimescale, float sendRate, double bufferTime, ref NetworkEma driftEma, ref NetworkEma deliveryTimeEma) where T : ISnapshot
        {
            if (buffer.Count == 0)
            {
                localTimeline = snapshot.remoteTime - bufferTime;
            }
            if (InsertIfNotExists(buffer, snapshot))
            {
                if (buffer.Count >= 2)
                {
                    double previousLocalTime = buffer.Values[buffer.Count - 2].localTime;
                    double finallyLocalTime = buffer.Values[buffer.Count - 1].localTime;
                    double localDeliveryTime = finallyLocalTime - previousLocalTime;
                    deliveryTimeEma.Add(localDeliveryTime);
                }
                double latestRemoteTime = snapshot.remoteTime;
                localTimeline = TimelineClamp(localTimeline, bufferTime, latestRemoteTime);
                double timeDiff = latestRemoteTime - localTimeline;
                driftEma.Add(timeDiff);
                double drift = driftEma.value - bufferTime;
                double absoluteNegativeThreshold = sendRate * NetworkSnapshot.snapshotSettings.catchupNegativeThreshold;
                double absolutePositiveThreshold = sendRate * NetworkSnapshot.snapshotSettings.catchupPositiveThreshold;
                localTimescale = Timescale(drift, NetworkSnapshot.snapshotSettings.catchupSpeed, NetworkSnapshot.snapshotSettings.slowdownSpeed, absoluteNegativeThreshold, absolutePositiveThreshold);
            }
        }
        
        /// <summary>
        /// 在有序的键值对列表（buffer）中插入一个快照
        /// </summary>
        /// <returns>根据长度返回是否插入成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InsertIfNotExists<T>(SortedList<double, T> buffer, T snapshot) where T : ISnapshot
        {
            int before = buffer.Count;
            buffer[snapshot.remoteTime] = snapshot;
            return buffer.Count > before;
        }
        
        /// <summary>
        /// 根据本地时间轴（localTimeline）、缓冲时间（bufferTime）和最新的远程时间（latestRemoteTime），
        /// 将本地时间轴限制在一个范围内。计算目标时间（targetTime）为最新远程时间减去缓冲时间，
        /// 然后计算下限（lowerBound）为目标时间减去缓冲时间，上限（upperBound）为目标时间加上缓冲时间
        /// 最后使用Math.Clamp方法将本地时间轴限制在下限和上限之间，并返回结果。
        /// </summary>
        private static double TimelineClamp(double localTimeline, double bufferTime, double latestRemoteTime)
        {
            double targetTime = latestRemoteTime - bufferTime;
            double lowerBound = targetTime - bufferTime;
            double upperBound = targetTime + bufferTime;
            return Math.Clamp(localTimeline, lowerBound, upperBound);
        }
        
        /// <summary>
        /// 动态调整时间缩放倍率
        /// </summary>
        /// <param name="drift"></param>
        /// <param name="catchupSpeed"></param>
        /// <param name="slowdownSpeed"></param>
        /// <param name="negativeThreshold"></param>
        /// <param name="positiveThreshold"></param>
        /// <returns></returns>
        private static double Timescale(double drift, double catchupSpeed, double slowdownSpeed, double negativeThreshold, double positiveThreshold)
        {
            return drift > positiveThreshold ? 1 + catchupSpeed : drift < negativeThreshold ? 1 - slowdownSpeed : 1;
        }
        
        /// <summary>
        /// 时间步长
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="localTimeline"></param>
        /// <param name="localTimescale"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StepTime(double deltaTime, ref double localTimeline, double localTimescale)  
        {
            localTimeline += deltaTime * localTimescale;
        }

        /// <summary>
        /// 差值步长
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="localTimeline"></param>
        /// <param name="fromSnapshot"></param>
        /// <param name="toSnapshot"></param>
        /// <param name="t"></param>
        /// <typeparam name="T"></typeparam>
        public static void StepInterpolation<T>(SortedList<double, T> buffer, double localTimeline, out T fromSnapshot, out T toSnapshot, out double t) where T : ISnapshot
        {
            Sample(buffer, localTimeline, out int origin, out int target, out t);
            fromSnapshot = buffer.Values[origin];
            toSnapshot = buffer.Values[target];
            for (int i = 0; i < origin && i < buffer.Count; ++i)
            {
                buffer.RemoveAt(0);
            }
        }

        /// <summary>
        /// 计算相邻两个远程时间差
        /// </summary>
        private static void Sample<T>(SortedList<double, T> buffer, double localTimeline, out int origin, out int target, out double t) where T : ISnapshot
        {
            for (int i = 0; i < buffer.Count - 1; ++i)
            {
                var first = buffer.Values[i];
                var second = buffer.Values[i + 1];
                if (localTimeline >= first.remoteTime && localTimeline <= second.remoteTime)
                {
                    origin = i;
                    target = i + 1;
                    t = InverseLerp(first.remoteTime, second.remoteTime, localTimeline);
                    return;
                }
            }

            origin = target = buffer.Values[0].remoteTime > localTimeline ? 0 : buffer.Count - 1;
            t = 0;
        }
        
        /// <summary>
        /// 计算差值
        /// </summary>
        private static double InverseLerp(double origin, double target, double value)
        {
            return Math.Abs(origin - target) > 0 ? Math.Clamp((value - origin) / (target - origin), 0, 1) : 0;
        }
        
        /// <summary>
        /// 差值步长
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="localTimeline"></param>
        /// <typeparam name="T"></typeparam>
        public static void StepInterpolation<T>(SortedList<double, T> buffer, double localTimeline) where T : ISnapshot
        {
            int origin = Sample(buffer, localTimeline);
            for (int i = 0; i < origin && i < buffer.Count; ++i)
            {
                buffer.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// 计算相邻两个远程时间差
        /// </summary>
        private static int Sample<T>(SortedList<double, T> buffer, double localTimeline) where T : ISnapshot
        {
            for (int i = 0; i < buffer.Count - 1; ++i)
            {
                var first = buffer.Values[i];
                var second = buffer.Values[i + 1];
                if (localTimeline >= first.remoteTime && localTimeline <= second.remoteTime)
                {
                    return i;
                }
            }

            return buffer.Values[0].remoteTime > localTimeline ? 0 : buffer.Count - 1;
        }
    }
}