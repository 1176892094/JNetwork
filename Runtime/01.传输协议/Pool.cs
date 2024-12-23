// *********************************************************************************
// # Project: JFramework
// # Unity: 6000.3.5f1
// # Author: 云谷千羽
// # Version: 1.0.0
// # History: 2024-11-30 17:11:02
// # Recently: 2024-12-22 20:12:06
// # Copyright: 2024, 云谷千羽
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