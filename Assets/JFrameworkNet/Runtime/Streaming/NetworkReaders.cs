using System;
using System.Collections.Generic;

namespace JFramework.Net
{
    public class NetworkReaders
    {
        /// <summary>
        /// 批处理队列
        /// </summary>
        private readonly Queue<NetworkWriter> writers = new Queue<NetworkWriter>();
        
        /// <summary>
        /// 读取器
        /// </summary>
        private readonly NetworkReader reader = new NetworkReader();

        /// <summary>
        /// 批处理数量
        /// </summary>
        public int Count => writers.Count;

        /// <summary>
        /// 远端时间戳
        /// </summary>
        private double timestamp;

        /// <summary>
        /// 开始读取批处理数据
        /// </summary>
        /// <param name="writer"></param>
        private void StartReadBatch(NetworkWriter writer)
        {
            reader.SetBuffer(writer.ToArraySegment());
            timestamp = reader.ReadDouble();
        }

        /// <summary>
        /// 将数据写入到writer并读取
        /// </summary>
        /// <param name="segment">批处理数据</param>
        /// <returns>返回处理成功</returns>
        public bool ReadEnqueue(ArraySegment<byte> segment)
        {
            if (segment.Count < NetworkConst.HeaderSize)
            {
                return false;
            }

            var writer = NetworkWriter.Pop();
            writer.WriteBytesInternal(segment.Array, segment.Offset, segment.Count);

            if (writers.Count == 0)
            {
                StartReadBatch(writer);
            }

            writers.Enqueue(writer); // 将数据放入队列末尾
            return true;
        }

        /// <summary>
        /// 读取队列中的数据并输出数据和时间戳
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public bool ReadDequeue(out NetworkReader reader, out double timestamp)
        {
            reader = null;
            if (writers.Count == 0)
            {
                timestamp = 0;
                return false;
            }

            if (this.reader.Capacity == 0)
            {
                timestamp = 0;
                return false;
            }

            if (this.reader.Residue == 0)
            {
                var writer = writers.Dequeue(); // 从队列中取出
                NetworkWriter.Push(writer);

                if (writers.Count > 0)
                {
                    var peek = writers.Peek();
                    StartReadBatch(peek);
                }
                else
                {
                    timestamp = 0;
                    return false;
                }
            }

            timestamp = this.timestamp;
            reader = this.reader;
            return true;
        }
    }
}