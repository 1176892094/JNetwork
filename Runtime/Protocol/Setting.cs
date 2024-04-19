using System;
using System.Collections.Generic;
using System.Net;

namespace JFramework.Udp
{
    public readonly struct Setting
    {
        public readonly int sendBufferSize;
        public readonly int receiveBufferSize;
        public readonly int maxUnit;
        public readonly int resend;
        public readonly int timeout;
        public readonly uint receivePacketSize;
        public readonly uint sendPacketSize;
        public readonly uint interval;
        public readonly bool noDelay;
        public readonly bool congestion;

        public Setting
        (
            int sendBufferSize = 1024 * 1024 * 7,
            int receiveBufferSize = 1024 * 1024 * 7,
            int maxUnit = Protocol.MTU_DEF,
            int timeout = Protocol.TIME_OUT,
            uint receivePacketSize = Protocol.WIN_RCV,
            uint sendPacketSize = Protocol.WIN_SND,
            uint interval = Protocol.INTERVAL,
            int resend = 0,
            bool noDelay = true,
            bool congestion = false)
        {
            this.receivePacketSize = receivePacketSize;
            this.sendPacketSize = sendPacketSize;
            this.timeout = timeout;
            this.maxUnit = maxUnit;
            this.sendBufferSize = sendBufferSize;
            this.receiveBufferSize = receiveBufferSize;
            this.interval = interval;
            this.resend = resend;
            this.noDelay = noDelay;
            this.congestion = congestion;
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