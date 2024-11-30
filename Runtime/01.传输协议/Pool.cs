// *********************************************************************************
// # Project: Forest
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-11-30  17:11
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System.Collections.Generic;

namespace JFramework.Udp
{
    internal sealed class Pool
    {
        private readonly Stack<Segment> segments = new Stack<Segment>();

        public Pool(int count)
        {
            for (var i = 0; i < count; ++i)
            {
                segments.Push(new Segment());
            }
        }

        public Segment Pop()
        {
            return segments.Count > 0 ? segments.Pop() : new Segment();
        }

        public void Push(Segment segment)
        {
            segment.Reset();
            segments.Push(segment);
        }
    }
}