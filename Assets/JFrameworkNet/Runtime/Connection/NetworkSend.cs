using System;
using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    public class NetworkSend
    {
        /// <summary>
        /// 批处理队列
        /// </summary>
        private readonly Queue<NetworkWriter> writers = new Queue<NetworkWriter>();

        /// <summary>
        /// 阈值
        /// </summary>
        private readonly int threshold;

        /// <summary>
        /// 批处理
        /// </summary>
        private NetworkWriter writer;

        /// <summary>
        /// 设置阈值
        /// </summary>
        /// <param name="threshold">传入阈值</param>
        public NetworkSend(int threshold) => this.threshold = threshold;

        /// <summary>
        /// 添加到队列末尾并写入数据
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="timeStamp">时间戳</param>
        public void WriteEnqueue(ArraySegment<byte> segment, double timeStamp)
        {
            if (writer != null && writer.position + segment.Count > threshold)
            {
                writers.Enqueue(writer); // 加入到队列中
                writer = null;
            }

            if (writer == null)
            {
                writer = NetworkWriterPool.Pop(); // 从对象池中取出
                writer.WriteDouble(timeStamp); // 重新写入时间戳
            }

            writer.WriteBytesInternal(segment.Array, segment.Offset, segment.Count); // 写入消息分段
        }

        /// <summary>
        /// 尝试从队列中取出元素并写入到目标
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool WriteDequeue(NetworkWriter target)
        {
            if (writers.TryDequeue(out var origin))
            {
                CopyToTarget(origin, target);
                return true;
            }

            if (writer == null)
            {
                return false;
            }

            CopyToTarget(writer, target);
            writer = null;
            return true;
        }

        /// <summary>
        /// 写入writer并将对象推入对象池
        /// </summary>
        private static void CopyToTarget(NetworkWriter origin, NetworkWriter target)
        {
            if (target.position != 0)
            {
                Debug.LogError($"Copy needs a empty writer!");
            }

            var segment = origin.ToArraySegment(); // 转化成数据分段
            target.WriteBytesInternal(segment.Array, segment.Offset, segment.Count); // 赋值到目标
            NetworkWriterPool.Push(origin); // 推入对象池
        }
    }
}