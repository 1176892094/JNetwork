using System;
using System.Collections.Generic;

namespace Transport
{
    internal class Jdp
    {
        private readonly Pool<Segment> segmentPool =
            new Pool<Segment>(() => new Segment(), segment => segment.Reset(), 32);

        private uint identity;
        private readonly uint maxTransmitUnit; // 最大传输单元（Maximum Transmit Unit）
        private readonly uint maxSegmentSize; // 最大分段大小（Maximum Segment Size）
        private readonly uint packageReceive; // 接收包大小
        private readonly Action<int, byte[]> onUpdate;
        private readonly Queue<Segment> sendQueue = new Queue<Segment>(16); // 发送队列，用于存储待发送的数据包

        public Jdp(uint identity, Action<int, byte[]> onUpdate)
        {
            this.identity = identity;
            this.onUpdate = onUpdate;
            packageReceive = Utils.PackageReceive; // 128
            maxTransmitUnit = Utils.MaxTransferUnit; // 1200
            maxSegmentSize = maxTransmitUnit - Utils.Overhead; // 1176 = 1200 - 24
        }

        public int Send(byte[] buffer, int offset, int length)
        {
            if (length < 0)
            {
                return -1;
            }

            int count;

            if (length > maxSegmentSize)
            {
                count = (int)((length + maxSegmentSize - 1) / maxSegmentSize);
            }
            else
            {
                count = 1;
            }

            if (count > Utils.MaxFragment) //count不能大于最大分段 255
            {
                throw new Exception($"Send length = {length} requires {count} fragments.");
            }

            if (count >= packageReceive)
            {
                return -2;
            }

            for (int i = 0; i < count; i++)
            {
                int size = length > (int)maxSegmentSize ? (int)maxSegmentSize : length;
                var segment = segmentPool.Pop();

                if (length > 0)
                {
                    segment.data.Write(buffer, offset, size);
                }

                segment.fragment = (uint)(count - i - 1);
                sendQueue.Enqueue(segment);
                offset += size;
                length -= size;
            }

            return 0;
        }
    }
}