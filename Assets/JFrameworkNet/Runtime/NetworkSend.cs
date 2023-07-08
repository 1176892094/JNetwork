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
        private readonly Queue<NetworkWriter> batches = new Queue<NetworkWriter>();
        
        /// <summary>
        /// 阈值
        /// </summary>
        private readonly int threshold;
        
        /// <summary>
        /// 批处理
        /// </summary>
        private NetworkWriter batch;
        
        /// <summary>
        /// 设置阈值
        /// </summary>
        /// <param name="threshold">传入阈值</param>
        public NetworkSend(int threshold) => this.threshold = threshold;

        /// <summary>
        /// 添加到队列末尾并写入数据
        /// </summary>
        public void WriteEnqueue(ArraySegment<byte> message, double timeStamp)
        {
            if (batch != null && batch.position + message.Count > threshold)
            {
                batches.Enqueue(batch);
                batch = null;
            }

            if (batch == null)
            {
                batch = NetworkWriterPool.Pop(); // 从对象池中取出
                batch.WriteDouble(timeStamp);
            }

            batch.WriteBytesInternal(message.Array, message.Offset, message.Count);
        }
        
        /// <summary>
        /// 尝试从队列中取出元素并写入writer
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        public bool WriteDequeue(NetworkWriter writer)
        {
            if (batches.TryDequeue(out var writerObject))
            {
                CopyAndWrite(writerObject, writer); 
                return true;
            }

            if (batch == null)
            {
                Debug.Log("false");
                return false;
            }

            Debug.Log(batch);
            CopyAndWrite(batch, writer);
            batch = null;
            return true;
        }

        /// <summary>
        /// 写入writer并将对象推入对象池
        /// </summary>
        private static void CopyAndWrite(NetworkWriter batch, NetworkWriter writer)
        {
            if (writer.position != 0)
            {
                throw new ArgumentException($"Copy needs a empty writer!");
            }

            var segment = batch.ToArraySegment();
            writer.WriteBytesInternal(segment.Array, segment.Offset, segment.Count);
            NetworkWriterPool.Push(batch); // 推入对象池
        }
    }
}