using System;
using System.Collections.Generic;

namespace JFramework.Udp
{
    public class Protocol
    {
        public const int RTO_NDL = 30;
        public const int RTO_MIN = 100;
        public const int RTO_DEF = 200;
        public const int RTO_MAX = 60000;
        public const int CMD_PUSH = 81;
        public const int CMD_ACK = 82;
        public const int CMD_W_ASK = 83;
        public const int CMD_W_INS = 84;
        public const int ASK_SEND = 1;
        public const int ASK_TELL = 2;
        public const int WND_SND = 32;
        public const int WND_RCV = 128;
        public const int MTU_DEF = 1200;
        public const int INTERVAL = 100;
        public const int OVERHEAD = 24;
        public const int FRG_MAX = byte.MaxValue;
        public const int DEAD_LINK = 20;
        public const int TIME_OUT = 10000;
        public const int THRESH_INIT = 2;
        public const int THRESH_MIN = 2;
        public const int PROBE_INIT = 7000;
        public const int PROBE_LIMIT = 120000;
        public const int FAST_ACK_LIMIT = 5;

        internal int state;
        internal uint mtu;
        internal uint mss;
        internal uint snd_una;
        internal uint snd_nxt;
        internal uint rcv_nxt;
        internal uint ss_thresh;
        internal int rx_rtt_val;
        internal int rx_s_rtt;
        internal int rx_rto;
        internal int rx_min_rto;
        internal uint snd_wnd;
        internal uint rcv_wnd;
        internal uint rmt_wnd;
        internal uint cmd_wnd;
        internal uint probe;
        internal uint interval;
        internal uint ts_flush;
        internal bool updated;
        internal uint ts_probe;
        internal uint probe_wait;
        internal uint dead_link;
        internal uint incr;
        internal uint current;
        internal uint fast_resend;
        internal uint no_delay;
        internal bool noc_wnd;
        internal byte[] buffer;
        private readonly uint conv;
        internal readonly int fast_limit;
        private readonly Action<byte[], int> output;
        private readonly Pool segmentPool = new Pool(32);
        internal readonly List<AckItem> ackList = new List<AckItem>(16);
        internal readonly List<Segment> sendBuffer = new List<Segment>(16);
        internal readonly List<Segment> receiveBuffer = new List<Segment>(16);
        internal readonly Queue<Segment> sendQueue = new Queue<Segment>(16);
        internal readonly Queue<Segment> receiveQueue = new Queue<Segment>(16);

        internal struct AckItem
        {
            public readonly uint sendId;
            public readonly uint sendTime;

            public AckItem(uint sendId, uint sendTime)
            {
                this.sendId = sendId;
                this.sendTime = sendTime;
            }
        }

        public Protocol(uint conv, Action<byte[], int> output)
        {
            this.conv = conv;
            this.output = output;
            snd_wnd = WND_SND;
            rcv_wnd = WND_RCV;
            rmt_wnd = WND_RCV;
            mtu = MTU_DEF;
            mss = mtu - OVERHEAD;
            rx_rto = RTO_DEF;
            rx_min_rto = RTO_MIN;
            interval = INTERVAL;
            ts_flush = INTERVAL;
            ss_thresh = THRESH_INIT;
            fast_limit = FAST_ACK_LIMIT;
            dead_link = DEAD_LINK;
            buffer = new byte[(mtu + OVERHEAD) * 3];
        }
        
        public int Receive(byte[] buffer, int length)
        {
            if (length < 0)
            {
                throw new NotSupportedException("Receive is peek for negative length is not supported!");
            }

            if (receiveQueue.Count == 0)
            {
                return -1;
            }

            var peekSize = PeekSize();
            if (peekSize < 0)
            {
                return -2;
            }

            if (peekSize > length)
            {
                return -3;
            }

            var recover = receiveQueue.Count >= rcv_wnd;

            length = 0;
            var offset = 0;

            while (receiveQueue.Count > 0)
            {
                var segment = receiveQueue.Dequeue();
                Buffer.BlockCopy(segment.data.GetBuffer(), 0, buffer, offset, (int)segment.data.Position);
                offset += (int)segment.data.Position;

                length += (int)segment.data.Position;
                var fragment = segment.fragment;
                segmentPool.Push(segment);

                if (fragment == 0)
                {
                    break;
                }
            }


            var removed = 0;
            foreach (var segment in receiveBuffer)
            {
                if (segment.sendId == rcv_nxt && receiveQueue.Count < rcv_wnd)
                {
                    ++removed;
                    receiveQueue.Enqueue(segment);
                    rcv_nxt++;
                }
                else
                {
                    break;
                }
            }

            receiveBuffer.RemoveRange(0, removed);


            if (receiveQueue.Count < rcv_wnd && recover)
            {
                probe |= ASK_TELL;
            }

            return length;
        }


        public int PeekSize()
        {
            var length = 0;
            if (receiveQueue.Count == 0)
            {
                return -1;
            }

            var segment = receiveQueue.Peek();
            if (segment.fragment == 0)
            {
                return (int)segment.data.Position;
            }

            if (receiveQueue.Count < segment.fragment + 1)
            {
                return -1;
            }

            foreach (var seg in receiveQueue)
            {
                length += (int)seg.data.Position;
                if (seg.fragment == 0)
                {
                    break;
                }
            }

            return length;
        }


        public int Send(byte[] buffer, int offset, int length)
        {
            if (length < 0)
            {
                return -1;
            }

            int count;
            if (length <= mss)
            {
                count = 1;
            }
            else
            {
                count = (int)((length + mss - 1) / mss);
            }


            if (count > FRG_MAX)
            {
                throw new Exception($"Send length={length} requires {count} fragments, but kcp can only handle up to {FRG_MAX} fragments.");
            }

            if (count >= rcv_wnd)
            {
                return -2;
            }

            if (count == 0)
            {
                count = 1;
            }

            for (int i = 0; i < count; i++)
            {
                var size = length > (int)mss ? (int)mss : length;
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


        private void UpdateAck(int rtt)
        {
            if (rx_s_rtt == 0)
            {
                rx_s_rtt = rtt;
                rx_rtt_val = rtt / 2;
            }
            else
            {
                int delta = rtt - rx_s_rtt;
                if (delta < 0)
                {
                    delta = -delta;
                }

                rx_rtt_val = (3 * rx_rtt_val + delta) / 4;
                rx_s_rtt = (7 * rx_s_rtt + rtt) / 8;
                if (rx_s_rtt < 1) rx_s_rtt = 1;
            }

            var rto = rx_s_rtt + Math.Max((int)interval, 4 * rx_rtt_val);
            rx_rto = Math.Clamp(rto, rx_min_rto, RTO_MAX);
        }

        internal void ShrinkBuffer()
        {
            snd_una = sendBuffer.Count > 0 ? sendBuffer[0].sendId : snd_nxt;
        }

        internal void ParseAck(uint sendId)
        {
            if (Utility.Compare(sendId, snd_una) < 0 || Utility.Compare(sendId, snd_nxt) >= 0)
            {
                return;
            }

            for (int i = 0; i < sendBuffer.Count; ++i)
            {
                var segment = sendBuffer[i];
                if (sendId == segment.sendId)
                {
                    sendBuffer.RemoveAt(i);
                    segmentPool.Push(segment);
                    break;
                }

                if (Utility.Compare(sendId, segment.sendId) < 0)
                {
                    break;
                }
            }
        }

        internal void ParseUna(uint una)
        {
            int removed = 0;
            foreach (var segment in sendBuffer)
            {
                if (segment.sendId < una)
                {
                    ++removed;
                    segmentPool.Push(segment);
                }
                else
                {
                    break;
                }
            }

            sendBuffer.RemoveRange(0, removed);
        }
        
        internal void ParseFastAck(uint sendId, uint sendTime)
        {
            if (sendId < snd_una)
            {
                return;
            }

            if (sendId >= snd_nxt)
            {
                return;
            }

            foreach (var segment in sendBuffer)
            {
                if (sendId < segment.sendId)
                {
                    break;
                }

                if (sendId != segment.sendId)
                {
                    segment.fastAck++;
                }
            }
        }

        private void ParseData(Segment segment)
        {
            var sn = segment.sendId;
            if (Utility.Compare(sn, rcv_nxt + rcv_wnd) >= 0 || Utility.Compare(sn, rcv_nxt) < 0)
            {
                segmentPool.Push(segment);
                return;
            }

            InsertSegmentInReceiveBuffer(segment);
            MoveReceiveBufferReadySegmentsToQueue();
        }


        internal void InsertSegmentInReceiveBuffer(Segment segment)
        {
            int i;
            var repeat = false;
            for (i = receiveBuffer.Count - 1; i >= 0; i--)
            {
                var seg = receiveBuffer[i];
                if (seg.sendId == segment.sendId)
                {
                    repeat = true;
                    break;
                }

                if (Utility.Compare(segment.sendId, seg.sendId) > 0)
                {
                    break;
                }
            }

            if (!repeat)
            {
                receiveBuffer.Insert(i + 1, segment);
            }

            else
            {
                segmentPool.Push(segment);
            }
        }


        private void MoveReceiveBufferReadySegmentsToQueue()
        {
            var removed = 0;
            foreach (var segment in receiveBuffer)
            {
                if (segment.sendId == rcv_nxt && receiveQueue.Count < rcv_wnd)
                {
                    ++removed;
                    receiveQueue.Enqueue(segment);
                    rcv_nxt++;
                }
                else
                {
                    break;
                }
            }

            receiveBuffer.RemoveRange(0, removed);
        }


        public int Input(byte[] data, int offset, int size)
        {
            var flag = 0;
            var prev_una = snd_una;
            uint max_ack = 0;
            uint latest_ts = 0;

            if (data == null || size < OVERHEAD)
            {
                return -1;
            }

            while (true)
            {
                if (size < OVERHEAD)
                {
                    break;
                }

                offset += Utility.Decode32U(data, offset, out uint conv_);
                if (conv_ != conv) return -1;
                offset += Utility.Decode8U(data, offset, out byte command);
                offset += Utility.Decode8U(data, offset, out byte fragment);
                offset += Utility.Decode16U(data, offset, out ushort windowSize);
                offset += Utility.Decode32U(data, offset, out uint sendTime);
                offset += Utility.Decode32U(data, offset, out uint sendId);
                offset += Utility.Decode32U(data, offset, out uint una);
                offset += Utility.Decode32U(data, offset, out uint length);
                size -= OVERHEAD;

                if (size < length)
                {
                    return -2;
                }

                if (command != CMD_PUSH && command != CMD_ACK && command != CMD_W_ASK && command != CMD_W_INS)
                {
                    return -3;
                }

                rmt_wnd = windowSize;
                ParseUna(una);
                ShrinkBuffer();

                if (command == CMD_ACK)
                {
                    if (Utility.Compare(current, sendTime) >= 0)
                    {
                        UpdateAck(Utility.Compare(current, sendTime));
                    }

                    ParseAck(sendId);
                    ShrinkBuffer();
                    if (flag == 0)
                    {
                        flag = 1;
                        max_ack = sendId;
                        latest_ts = sendTime;
                    }
                    else
                    {
                        if (Utility.Compare(sendId, max_ack) > 0)
                        {
                            max_ack = sendId;
                            latest_ts = sendTime;
                        }
                    }
                }
                else if (command == CMD_PUSH)
                {
                    if (Utility.Compare(sendId, rcv_nxt + rcv_wnd) < 0)
                    {
                        ackList.Add(new AckItem(sendId, sendTime));
                        if (Utility.Compare(sendId, rcv_nxt) >= 0)
                        {
                            var segment = segmentPool.Pop();
                            segment.conv = conv_;
                            segment.command = command;
                            segment.fragment = fragment;
                            segment.windowSize = windowSize;
                            segment.sendTime = sendTime;
                            segment.sendId = sendId;
                            segment.una = una;
                            if (length > 0)
                            {
                                segment.data.Write(data, offset, (int)length);
                            }

                            ParseData(segment);
                        }
                    }
                }
                else if (command == CMD_W_ASK)
                {
                    probe |= ASK_TELL;
                }

                offset += (int)length;
                size -= (int)length;
            }

            if (flag != 0)
            {
                ParseFastAck(max_ack, latest_ts);
            }

            if (Utility.Compare(snd_una, prev_una) > 0)
            {
                if (cmd_wnd < rmt_wnd)
                {
                    if (cmd_wnd < ss_thresh)
                    {
                        cmd_wnd++;
                        incr += mss;
                    }
                    else
                    {
                        if (incr < mss)
                        {
                            incr = mss;
                        }

                        incr += mss * mss / incr + mss / 16;
                        if ((cmd_wnd + 1) * mss <= incr)
                        {
                            cmd_wnd = (incr + mss - 1) / ((mss > 0) ? mss : 1);
                        }
                    }

                    if (cmd_wnd > rmt_wnd)
                    {
                        cmd_wnd = rmt_wnd;
                        incr = rmt_wnd * mss;
                    }
                }
            }

            return 0;
        }
        
        
        private uint WndUnused()
        {
            if (receiveQueue.Count < rcv_wnd)
            {
                return rcv_wnd - (uint)receiveQueue.Count;
            }

            return 0;
        }

        private void MakeSpace(ref int size, int space)
        {
            if (size + space > mtu)
            {
                output(buffer, size);
                size = 0;
            }
        }

        private void FlushBuffer(int size)
        {
            if (size > 0)
            {
                output(buffer, size);
            }
        }

        public void Flush()
        {
            var size = 0;
            var lost = false;
            if (!updated)
            {
                return;
            }

            var seg = segmentPool.Pop();
            seg.conv = conv;
            seg.command = CMD_ACK;
            seg.windowSize = WndUnused();
            seg.una = rcv_nxt;

            foreach (var ack in ackList)
            {
                MakeSpace(ref size, OVERHEAD);
                seg.sendId = ack.sendId;
                seg.sendTime = ack.sendTime;
                size += seg.Encode(buffer, size);
            }

            ackList.Clear();

            if (rmt_wnd == 0)
            {
                if (probe_wait == 0)
                {
                    probe_wait = PROBE_INIT;
                    ts_probe = current + probe_wait;
                }
                else
                {
                    if (Utility.Compare(current, ts_probe) >= 0)
                    {
                        if (probe_wait < PROBE_INIT)
                        {
                            probe_wait = PROBE_INIT;
                        }

                        probe_wait += probe_wait / 2;
                        if (probe_wait > PROBE_LIMIT)
                        {
                            probe_wait = PROBE_LIMIT;
                        }

                        ts_probe = current + probe_wait;
                        probe |= ASK_SEND;
                    }
                }
            }
            else
            {
                ts_probe = 0;
                probe_wait = 0;
            }


            if ((probe & ASK_SEND) != 0)
            {
                seg.command = CMD_W_ASK;
                MakeSpace(ref size, OVERHEAD);
                size += seg.Encode(buffer, size);
            }


            if ((probe & ASK_TELL) != 0)
            {
                seg.command = CMD_W_INS;
                MakeSpace(ref size, OVERHEAD);
                size += seg.Encode(buffer, size);
            }

            probe = 0;
            uint c_wnd_ = Math.Min(snd_wnd, rmt_wnd);
            if (!noc_wnd)
            {
                c_wnd_ = Math.Min(cmd_wnd, c_wnd_);
            }

            while (Utility.Compare(snd_nxt, snd_una + c_wnd_) < 0)
            {
                if (sendQueue.Count == 0)
                {
                    break;
                }

                var segment = sendQueue.Dequeue();
                segment.conv = conv;
                segment.command = CMD_PUSH;
                segment.windowSize = segment.windowSize;
                segment.sendTime = current;
                segment.sendId = snd_nxt;
                snd_nxt += 1;
                segment.una = rcv_nxt;
                segment.resendTime = current;
                segment.resendTimeout = rx_rto;
                segment.fastAck = 0;
                segment.resendCount = 0;
                sendBuffer.Add(segment);
            }

            var resent = fast_resend > 0 ? fast_resend : 0xffffffff;
            var rto_min = no_delay == 0 ? (uint)rx_rto >> 3 : 0;

            int change = 0;

            foreach (var segment in sendBuffer)
            {
                var needSend = false;

                if (segment.resendCount == 0)
                {
                    needSend = true;
                    segment.resendCount++;
                    segment.resendTimeout = rx_rto;
                    segment.resendTime = current + (uint)segment.resendTimeout + rto_min;
                }

                else if (Utility.Compare(current, segment.resendTime) >= 0)
                {
                    needSend = true;
                    segment.resendCount++;
                    if (no_delay == 0)
                    {
                        segment.resendTimeout += Math.Max(segment.resendTimeout, rx_rto);
                    }
                    else
                    {
                        int step = (no_delay < 2) ? segment.resendTimeout : rx_rto;
                        segment.resendTimeout += step / 2;
                    }

                    segment.resendTime = current + (uint)segment.resendTimeout;
                    lost = true;
                }

                else if (segment.fastAck >= resent)
                {
                    if (segment.resendCount <= fast_limit || fast_limit <= 0)
                    {
                        needSend = true;
                        segment.resendCount++;
                        segment.fastAck = 0;
                        segment.resendTime = current + (uint)segment.resendTimeout;
                        change++;
                    }
                }

                if (needSend)
                {
                    segment.sendTime = current;
                    segment.windowSize = seg.windowSize;
                    segment.una = rcv_nxt;

                    var need = OVERHEAD + (int)segment.data.Position;
                    MakeSpace(ref size, need);

                    size += segment.Encode(buffer, size);

                    if (segment.data.Position > 0)
                    {
                        Buffer.BlockCopy(segment.data.GetBuffer(), 0, buffer, size, (int)segment.data.Position);
                        size += (int)segment.data.Position;
                    }

                    if (segment.resendCount >= dead_link)
                    {
                        state = -1;
                    }
                }
            }

            segmentPool.Push(seg);
            FlushBuffer(size);


            if (change > 0)
            {
                var inflight = snd_nxt - snd_una;
                ss_thresh = inflight / 2;
                if (ss_thresh < THRESH_MIN)
                {
                    ss_thresh = THRESH_MIN;
                }

                cmd_wnd = ss_thresh + resent;
                incr = cmd_wnd * mss;
            }


            if (lost)
            {
                ss_thresh = c_wnd_ / 2;
                if (ss_thresh < THRESH_MIN)
                {
                    ss_thresh = THRESH_MIN;
                }

                cmd_wnd = 1;
                incr = mss;
            }

            if (cmd_wnd < 1)
            {
                cmd_wnd = 1;
                incr = mss;
            }
        }


        public void Update(uint currentTime)
        {
            current = currentTime;
            if (!updated)
            {
                updated = true;
                ts_flush = current;
            }

            int slap = Utility.Compare(current, ts_flush);
            if (slap >= 10000 || slap < -10000)
            {
                ts_flush = current;
                slap = 0;
            }

            if (slap >= 0)
            {
                ts_flush += interval;
                if (current >= ts_flush)
                {
                    ts_flush = current + interval;
                }

                Flush();
            }
        }

        public void SetMtu(uint mtu)
        {
            if (mtu < 50)
            {
                throw new ArgumentException("MTU must be higher than 50 and higher than OVERHEAD");
            }

            buffer = new byte[(mtu + OVERHEAD) * 3];
            this.mtu = mtu;
            mss = mtu - OVERHEAD;
        }

        public void SetNoDelay(uint no_delay, uint interval = INTERVAL, uint resend = 0, bool noc_wnd = false)
        {
            this.no_delay = no_delay;
            rx_min_rto = no_delay != 0 ? RTO_NDL : RTO_MIN;

            if (interval > 5000)
            {
                interval = 5000;
            }
            else if (interval < 10)
            {
                interval = 10;
            }

            this.interval = interval;
            fast_resend = resend;
            this.noc_wnd = noc_wnd;
        }


        public void SetWindowSize(uint sendWindow, uint receiveWindow)
        {
            if (sendWindow > 0)
            {
                snd_wnd = sendWindow;
            }

            if (receiveWindow > 0)
            {
                rcv_wnd = Math.Max(receiveWindow, WND_RCV);
            }
        }
    }
}