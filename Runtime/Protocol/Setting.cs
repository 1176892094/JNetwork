using System;
using System.Collections.Generic;
using System.Net;

namespace JFramework.Udp
{
    public readonly struct Setting
    {
        public readonly int unit;
        public readonly int timeout;
        public readonly uint receive;
        public readonly uint send;
        public readonly uint resend;
        public readonly uint interval;

        public Setting(int unit = Protocol.MTU_MAX, int timeout = Protocol.TIMEOUT, uint send = Protocol.WIN_SED, uint receive = Protocol.WIN_REV, uint resend = Protocol.RESEND, uint interval = Protocol.TIME)
        {
            this.unit = unit;
            this.timeout = timeout;
            this.send = send;
            this.receive = receive;
            this.resend = resend;
            this.interval = interval;
        }
    }

    public struct Message
    {
        public readonly uint sendId;
        public readonly uint sendTime;

        public Message(uint sendId, uint sendTime)
        {
            this.sendId = sendId;
            this.sendTime = sendTime;
        }
    }

    internal struct Proxies
    {
        public Proxy proxy;
        public readonly EndPoint endPoint;

        public Proxies(EndPoint endPoint)
        {
            proxy = null;
            this.endPoint = endPoint;
        }
    }


    public static class Log
    {
        public static Action<string> Info = Console.WriteLine;
        public static Action<string> Warn = Console.WriteLine;
        public static Action<string> Error = Console.Error.WriteLine;
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

        public Segment Pop() => segments.Count > 0 ? segments.Pop() : new Segment();

        public void Push(Segment segment)
        {
            segment.Reset();
            segments.Push(segment);
        }
    }
}