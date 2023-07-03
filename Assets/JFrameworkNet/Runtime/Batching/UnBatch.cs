using System;
using System.Collections.Generic;

namespace JFramework.Net
{
   public class UnBatch
    {
        private readonly NetworkReader reader = new NetworkReader(Array.Empty<byte>());
        private readonly Queue<NetworkWriterObject> batches = new Queue<NetworkWriterObject>();
        public int BatchesCount => batches.Count;
        private double readerRemoteTimeStamp;

        private void StartReadingBatch(NetworkWriterObject batch)
        {
            reader.SetBuffer(batch.ToArraySegment());
            readerRemoteTimeStamp = reader.ReadDouble();
        }
        
        public bool AddBatch(ArraySegment<byte> batch)
        {
            if (batch.Count < NetworkConst.HeaderSize) return false;
            var writer = NetworkWriterPool.Pop();
            writer.WriteBytes(batch.Array, batch.Offset, batch.Count);
            
            if (batches.Count == 0)
            {
                StartReadingBatch(writer);
            }
            
            batches.Enqueue(writer);
            return true;
        }
        
        public bool GetNextMessage(out NetworkReader message, out double remoteTimeStamp)
        {
            message = null;
            if (batches.Count == 0)
            {
                remoteTimeStamp = 0;
                return false;
            }
            
            if (reader.Capacity == 0)
            {
                remoteTimeStamp = 0;
                return false;
            }
            
            if (reader.Remaining == 0)
            {
                var writerPool = batches.Dequeue();
                NetworkWriterPool.Push(writerPool);
                
                if (batches.Count > 0)
                {
                    var writerObject = batches.Peek();
                    StartReadingBatch(writerObject);
                }
                else
                {
                    remoteTimeStamp = 0;
                    return false;
                }
            }
            
            remoteTimeStamp = readerRemoteTimeStamp;
            message = reader;
            return true;
        }
    }
}