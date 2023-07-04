using System;
using System.Collections.Generic;

namespace JFramework.Net
{
    public class NetworkReceive
    {
        /// <summary>
        /// 读取器
        /// </summary>
        private readonly NetworkReader reader = new NetworkReader(Array.Empty<byte>());

        /// <summary>
        /// 批处理队列
        /// </summary>
        private readonly Queue<NetworkWriterObject> batches = new Queue<NetworkWriterObject>();

        /// <summary>
        /// 批处理数量
        /// </summary>
        public int batchCount => batches.Count;

        /// <summary>
        /// 远端时间戳
        /// </summary>
        private double remoteTimestamp;

        /// <summary>
        /// 开始读取批处理数据
        /// </summary>
        /// <param name="batch"></param>
        private void StartReadBatch(NetworkWriter batch)
        {
            reader.SetBuffer(batch.ToArraySegment());
            remoteTimestamp = reader.ReadDouble();
        }

        /// <summary>
        /// 将数据写入到writer并读取
        /// </summary>
        /// <param name="batch">批处理数据</param>
        /// <returns>返回处理成功</returns>
        public bool ReadEnqueue(ArraySegment<byte> batch)
        {
            if (batch.Count < NetworkConst.HeaderSize)
            {
                return false;
            }

            var writer = NetworkWriterPool.Pop();
            writer.WriteBytes(batch.Array, batch.Offset, batch.Count);

            if (batches.Count == 0)
            {
                StartReadBatch(writer);
            }

            batches.Enqueue(writer); // 将数据放入队列末尾
            return true;
        }

        /// <summary>
        /// 读取队列中的数据并输出数据和时间戳
        /// </summary>
        /// <param name="message"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public bool ReadDequeue(out NetworkReader message, out double timestamp)
        {
            message = null;
            if (batches.Count == 0)
            {
                timestamp = 0;
                return false;
            }

            if (reader.Capacity == 0)
            {
                timestamp = 0;
                return false;
            }

            if (reader.Remaining == 0)
            {
                var writer = batches.Dequeue(); // 从队列中取出
                NetworkWriterPool.Push(writer);

                if (batches.Count > 0)
                {
                    var writerObject = batches.Peek();
                    StartReadBatch(writerObject);
                }
                else
                {
                    timestamp = 0;
                    return false;
                }
            }

            timestamp = remoteTimestamp;
            message = reader;
            return true;
        }
    }
}