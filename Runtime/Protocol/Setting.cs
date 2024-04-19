using System;
using System.Collections.Generic;
using System.Net;

namespace JFramework.Udp
{
    public readonly struct Setting
    {
        public readonly int sendBuffer;
        public readonly int receiveBuffer;
        public readonly int maxUnit;
        public readonly uint resend;
        public readonly int timeout;
        public readonly uint receiveSize;
        public readonly uint sendSize;
        public readonly uint interval;

        public Setting
        (
            int sendBuffer = 1024 * 1024 * 7,
            int receiveBuffer = 1024 * 1024 * 7,
            int maxUnit = Protocol.MTU_DEF,
            int timeout = 10000,
            uint receiveSize = Protocol.WIN_RCV,
            uint sendSize = Protocol.WIN_SND,
            uint interval = Protocol.INTERVAL,
            uint resend = 0)
        {
            this.receiveSize = receiveSize;
            this.sendSize = sendSize;
            this.timeout = timeout;
            this.maxUnit = maxUnit;
            this.sendBuffer = sendBuffer;
            this.receiveBuffer = receiveBuffer;
            this.interval = interval;
            this.resend = resend;
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

    public struct Packet
    {
        public readonly uint sendId;
        public readonly uint sendTime;

        public Packet(uint sendId, uint sendTime)
        {
            this.sendId = sendId;
            this.sendTime = sendTime;
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