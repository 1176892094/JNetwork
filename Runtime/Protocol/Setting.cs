using System;
using System.Collections.Generic;

namespace JFramework.Udp
{
    [Serializable]
    public struct Setting
    {
        public int MaxUnit;
        public uint Timeout;
        public uint Interval;
        public uint DeadLink;
        public uint FastResend;
        public uint SendWindow;
        public uint ReceiveWindow;
        public bool NoDelay;
        public bool DualMode;
        public bool Congestion;

        public Setting(
            int MaxUnit = Protocol.MTU_DEF,
            uint Timeout = Protocol.TIME_OUT,
            uint Interval = 10,
            uint DeadLink = Protocol.DEAD_LINK,
            uint FastResend = 0,
            uint SendWindow = Protocol.WND_SND,
            uint ReceiveWindow = Protocol.WND_RCV,
            bool NoDelay = true,
            bool DualMode = true,
            bool Congestion = false)
        {
            this.MaxUnit = MaxUnit;
            this.Timeout = Timeout;
            this.Interval = Interval;
            this.DeadLink = DeadLink;
            this.FastResend = FastResend;
            this.SendWindow = SendWindow;
            this.ReceiveWindow = ReceiveWindow;
            this.NoDelay = NoDelay;
            this.DualMode = DualMode;
            this.Congestion = Congestion;
        }
    }

    public static class Log
    {
        public static Action<string> Info = Console.WriteLine;
        public static Action<string> Warn = Console.WriteLine;
        public static Action<string> Error = Console.Error.WriteLine;
    }

    internal static class Channel
    {
        public const int Reliable = 1;
        public const int Unreliable = 2;
    }

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