using System.Collections.Generic;

namespace JFramework.Udp
{
    internal sealed class SegmentPool
    {
        private readonly Stack<Segment> segments = new Stack<Segment>();

        public SegmentPool(int capacity)
        {
            for (var i = 0; i < capacity; ++i)
            {
                segments.Push(new Segment());
            }
        }

        public Segment Pop() => segments.Count > 0 ? segments.Pop() : new Segment();

        public void Push(Segment segment)
        {
            segment.Reset();
            segments.Push(segment);
        }
    }
}