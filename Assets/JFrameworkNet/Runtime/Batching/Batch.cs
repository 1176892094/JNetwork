using System;
using System.Collections.Generic;

namespace JFramework.Net
{
    public class Batch
    {
        private readonly Queue<NetworkWriterObject> batches = new Queue<NetworkWriterObject>();
        private readonly int threshold;
        private NetworkWriterObject batch;
        public Batch(int threshold) => this.threshold = threshold;

        public void Enqueue(ArraySegment<byte> message, double timeStamp)
        {
            if (batch != null && batch.position + message.Count > threshold)
            {
                batches.Enqueue(batch); // 将元素添加到队列末尾
                batch = null;
            }

            if (batch == null)
            {
                batch = NetworkWriterPool.Pop(); // 从对象池中取出
                batch.WriteDouble(timeStamp);
            }

            batch.WriteBytes(message.Array, message.Offset, message.Count);
        }
        
        public bool Dequeue(NetworkWriter writer)
        {
            if (batches.TryDequeue(out var writerObject))
            {
                CopyAndPush(writerObject, writer); //尝试从队列中取出元素
                return true;
            }

            if (batch == null)
            {
                return false;
            }

            CopyAndPush(batch, writer);
            batch = null;
            return true;
        }

        private static void CopyAndPush(NetworkWriterObject batch, NetworkWriter writer)
        {
            if (writer.position != 0)
            {
                throw new ArgumentException($"GetBatch needs a fresh writer!");
            }

            var segment = batch.ToArraySegment();
            writer.WriteBytes(segment.Array, segment.Offset, segment.Count);
            NetworkWriterPool.Push(batch); // 推入对象池
        }
    }
}