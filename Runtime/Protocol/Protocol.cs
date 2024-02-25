using System;
using System.Collections.Generic;
using System.Linq;

namespace JFramework.Udp
{
    internal sealed class Protocol
    {
        private struct Packet
        {
            public uint serialNumber;
            public uint timestamp;
        }
        
        public const int FRG_MAX = 255;            // 分片的最大数量，使用1字节进行编码，最大为255
        public const int TIME_OUT = 10000;         // 超时时间
        public const int WIN_SND = 32;             // 默认的发送窗口大小
        public const int WIN_RCV = 128;            // 默认的接收窗口大小，必须大于等于最大分片大小
        public const int MTU_DEF = 1200;           // 默认的最大传输单元MTU
        public const int OVERHEAD = 24;            // 数据包头部的额外开销
        public const int INTERVAL = 100;           // 内部处理时钟的间隔时间
        private const int RTO_NDL = 30;            // 无延迟的最小重传超时时间
        private const int RTO_MIN = 100;           // 最小RTO
        private const int RTO_DEF = 200;           // 默认RTO
        private const int RTO_MAX = 60000;         // 最大RTO
        private const int CMD_PUSH = 81;           // 推送数据命令
        private const int CMD_ACK  = 82;           // 确认收到数据命令
        private const int CMD_WIN_ASK = 83;        // 窗口探测请求命令
        private const int CMD_WIN_INS = 84;        // 窗口大小插入命令
        private const int ASK_SEND = 1;            // 需要发送的 CMD_WIN_ASK 命令
        private const int ASK_TELL = 2;            // 需要发送的 CMD_WIN_INS 命令
        private const int DEAD_LINK = 20;          // 当一个段被认为丢失之前的最大重传次数
        private const int THRESH_INIT = 2;         // 拥塞窗口增长阈值的初始值
        private const int THRESH_MIN = 2;          // 拥塞窗口增长阈值的最小值
        private const int PROBE_INIT = 7000;       // 探测窗口大小的初始时间（7秒）
        private const int PROBE_LIMIT = 120000;    // 探测窗口大小的最长时间（120秒）
        private const int FAST_ACK_LIMIT = 5;      // 触发快速重传的最大次数
        public int state;                          // Jdp状态
        private int fastResend;                    // 快速重传的触发次数
        private int roundTripTime;                 // RTT的平均偏差，用于衡量RTT的抖动
        private int roundTripTimeSmooth;           // 平滑的往返时间（RTT）的加权平均值
        private int receiveRto;                    // 接收方的RTO
        private int receiveMinRto;                 // 接收方的最小RTO
        private uint maxTransferUnit;              // 最大传输单元
        private uint maxSegmentSize;               // 最大分段大小
        private uint unAcknowledge;                // 未确认的序号，例为9表示8已经被确认，9和10已经发送。
        private uint sendNextSegment;              // 发送数据的下一个序号，不断增加
        private uint receiveNextSegment;           // 接收数据的下一个序号，不断增加
        private uint slowStartThreshold;           // 慢启动阈值
        private uint sendWindowSize;               // 发送窗口大小
        private uint receiveWindowSize;            // 接收窗口大小
        private uint remoteWindowSize;             // 远端窗口大小
        private uint congestionWindowSize;         // 拥塞窗口大小
        private uint probe;                        // 探测标志
        private uint interval;                     // 内部处理时钟的间隔时间
        private uint timestampFlush;               // 最后一次刷新数据的时间戳（毫秒）
        private uint noDelay;                      // 是否启用无延迟模式
        private bool updated;                      // 是否更新
        private uint probeTimestamp;               // 探测窗口的时间戳
        private uint probeWaitTime;                // 等待探测的时间
        private uint increment;                    // 拥塞窗口增加的步进值
        private uint currentTime;                  // 当前时间（毫秒），由Update方法设置
        private bool noCongestionWindow;           // 是否启用拥塞控制
        private byte[] buffer;                     // 接收缓存
        public readonly uint deadLink;             // 一个分段被认为丢失之前的最大重传次数
        private readonly int fastLimit;            // 快速重传的最大限制次数
        private readonly uint conversation;        // 会话Id
        private readonly Action<byte[], int> onOutput;
        private readonly SegmentPool segmentPool = new SegmentPool(32);
        private readonly List<Packet> packetList = new List<Packet>(16);
        private readonly List<Segment> sendBuffers = new List<Segment>(16);
        private readonly List<Segment> receiveBuffers = new List<Segment>(16);
        public readonly Queue<Segment> sendQueue = new Queue<Segment>(16); 
        private readonly Queue<Segment> receiveQueue = new Queue<Segment>(16);
        
        public Protocol(uint conversation, Action<byte[], int> onOutput)
        {
            this.conversation = conversation;
            this.onOutput = onOutput;
            sendWindowSize = WIN_SND;
            receiveWindowSize = WIN_RCV;
            remoteWindowSize = WIN_RCV;
            maxTransferUnit = MTU_DEF;
            maxSegmentSize = maxTransferUnit - OVERHEAD;
            receiveRto = RTO_DEF;
            receiveMinRto = RTO_MIN;
            interval = INTERVAL;
            timestampFlush = INTERVAL;
            slowStartThreshold = THRESH_INIT;
            fastLimit = FAST_ACK_LIMIT;
            deadLink = DEAD_LINK;
            buffer = new byte[(maxTransferUnit + OVERHEAD) * 3];
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="buffer">传入的数据</param>
        /// <param name="length">传入的长度</param>
        /// <returns>返回读取的字节数</returns>
        public int Receive(byte[] buffer, int length)
        {
            if (length < 0)
            {
                throw new NotSupportedException("Length is not supported!");
            }

            if (receiveQueue.Count == 0)
            {
                return -1;
            }

            int peekSize = PeekSize();

            if (peekSize < 0)
            {
                return -2;
            }

            if (peekSize > length)
            {
                return -3;
            }

            bool recover = receiveQueue.Count >= receiveWindowSize;

            int offset = 0;
            length = 0;
            while (receiveQueue.Count > 0)
            {
                var segment = receiveQueue.Dequeue();
                // 复制分段数据到我们的缓冲区
                Buffer.BlockCopy(segment.stream.GetBuffer(), 0, buffer, offset, (int)segment.stream.Position);
                offset += (int)segment.stream.Position;
                length += (int)segment.stream.Position;
                var fragment = segment.fragment;
                segmentPool.Push(segment); // 回收进对象池
                if (fragment == 0)
                {
                    break;
                }
            }
            
            MoveReceiveBufferToQueue();
            if (receiveQueue.Count < receiveWindowSize && recover)
            {
                probe |= ASK_TELL;
            }

            return length;
        }

        public int PeekSize()
        {
            int length = 0;
            if (receiveQueue.Count == 0)
            {
                return -1; //队列为空
            }

            var segment = receiveQueue.Peek();
            if (segment.fragment == 0)
            {
                // 表示消息不需要分片，此段的大小就是消息的最终大小，将其作为返回值。
                return (int)segment.stream.Position;
            }
            
            if (receiveQueue.Count < segment.fragment + 1)
            {
                // 有分片未接收完整
                return -1;
            }
            
            foreach (Segment receive in receiveQueue) // 累加分片长度
            {
                length += (int)receive.stream.Position;
                if (receive.fragment == 0)
                {
                    break; // 表示该分片是最后一个分片，退出循环
                }
            }

            return length;
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="buffer">发送数据</param>
        /// <param name="offset">偏移量</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        public int Send(byte[] buffer, int offset, int length)
        {
            if (length < 0)
            {
                return -1;
            }

            // 根据消息长度和最大传输单元的关系计算出分片数量
            var count = length <= maxSegmentSize ? 1 : (int)((length + maxSegmentSize - 1) / maxSegmentSize); // 分段数量

            if (count > FRG_MAX)
            {
                throw new Exception($"Send len = {length} requires {count} fragments.");
            }
            
            if (count >= receiveWindowSize)
            {
                return -2; // 不能大于接收窗口的大小
            }

            if (count == 0)
            {
                count = 1;
            }
            
            for (int i = 0; i < count; i++) // 写入分段数量
            {
                int size = length > (int)maxSegmentSize ? (int)maxSegmentSize : length;
                var segment = segmentPool.Pop();

                if (length > 0)
                {
                    segment.stream.Write(buffer, offset, size);
                }
               
                segment.fragment = (uint)(count - i - 1);
                sendQueue.Enqueue(segment);
                offset += size;
                length -= size;
            }

            return 0;
        }

        /// <summary>
        /// 计算新的 RTO 和 RTT
        /// </summary>
        /// <param name="roundTrip"></param>
        private void UpdateAcknowledge(int roundTrip)
        {
            if (roundTripTimeSmooth == 0)
            {
                roundTripTimeSmooth = roundTrip;
                roundTripTime = roundTrip / 2;
            }
            else
            {
                int delta = Math.Abs(roundTrip - roundTripTimeSmooth);
                roundTripTime = (3 * roundTripTime + delta) / 4;
                roundTripTimeSmooth = (7 * roundTripTimeSmooth + roundTrip) / 8;
                if (roundTripTimeSmooth < 1) roundTripTimeSmooth = 1;
            }

            int rto = roundTripTimeSmooth + Math.Max((int)interval, 4 * roundTripTime);
            receiveRto = Math.Clamp(rto, receiveMinRto, RTO_MAX);
        }
        
        private void ShrinkBuffer()
        {
            if (sendBuffers.Count > 0)
            {
                Segment seg = sendBuffers[0];
                unAcknowledge = seg.serialNumber;
            }
            else
            {
                unAcknowledge = sendNextSegment;
            }
        }
        
        private void ParseAcknowledge(uint serialNumber)
        {
            if (Subtract(serialNumber, unAcknowledge) < 0 || Subtract(serialNumber, sendNextSegment) >= 0)
            {
                return;
            }
            
            for (int i = 0; i < sendBuffers.Count; ++i)
            {
                var segment = sendBuffers[i];
                if (serialNumber == segment.serialNumber)
                {
                    sendBuffers.RemoveAt(i);
                    segmentPool.Push(segment);
                    break;
                }
                
                if (Subtract(serialNumber, segment.serialNumber) < 0)
                {
                    break;
                }
            }
        }
        
        private void ParseUnAcknowledge(uint unAcknowledge)
        {
            int removed = 0;
            foreach (var segment in sendBuffers)
            {
                if (segment.serialNumber < unAcknowledge)
                {
                    removed++;
                    segmentPool.Push(segment);
                }
                else
                {
                    break;
                }
            }
            sendBuffers.RemoveRange(0, removed);
        }
        
        private void ParseFastAcknowledge(uint serialNumber)
        {
            if (serialNumber < unAcknowledge)
            {
                return;
            }

            if (serialNumber >= sendNextSegment)
            {
                return;
            }

            foreach (var segment in sendBuffers.TakeWhile(segment => serialNumber >= segment.serialNumber).Where(segment => serialNumber != segment.serialNumber))
            {
                segment.fastAcknowledge++;
            }
        }

        private void AcknowledgePush(uint serialNumber, uint timestamp)
        {
            packetList.Add(new Packet
            {
                serialNumber = serialNumber, 
                timestamp = timestamp
            });
        }

        private void ParseSegment(Segment segment)
        {
            uint serialNumber = segment.serialNumber;
            if (Subtract(serialNumber, receiveNextSegment + receiveWindowSize) >= 0 || Subtract(serialNumber, receiveNextSegment) < 0)
            {
                segmentPool.Push(segment);
                return;
            }

            InsertSegmentInReceiveBuffer(segment);
            MoveReceiveBufferToQueue();
        }

        /// <summary>
        /// 按照分片的序列号有序地插入接收缓冲区中
        /// </summary>
        /// <param name="segment">插入的分片</param>
        private void InsertSegmentInReceiveBuffer(Segment segment)
        {
            bool repeat = false;
            int index;
            for (index = receiveBuffers.Count - 1; index >= 0; index--)
            {
                Segment receive = receiveBuffers[index];
                if (receive.serialNumber == segment.serialNumber)
                {
                    repeat = true; // 找到了有相同序列号的分片
                    break;
                }

                if (Subtract(segment.serialNumber, receive.serialNumber) > 0)
                {
                    break;
                }
            }

            if (!repeat) // 判断是否存在重复的分片
            {
                receiveBuffers.Insert(index + 1, segment);
            }
            else
            {
                segmentPool.Push(segment); // 重复则进行回收
            }
        }
        
        /// <summary>
        /// 移动分段从 receiveBuffers 到 receiveQueue
        /// </summary>
        private void MoveReceiveBufferToQueue()
        {
            var removed = 0;
            foreach (var segment in receiveBuffers)
            {
                if (segment.serialNumber == receiveNextSegment && receiveQueue.Count < receiveWindowSize)
                {
                    removed++;
                    receiveQueue.Enqueue(segment);
                    receiveNextSegment++;
                }
                else
                {
                    break;
                }
            }

            receiveBuffers.RemoveRange(0, removed);
        }

        /// <summary>
        /// 当接收到底层数据包(例如UDP数据包)时使用
        /// </summary>
        public int Input(byte[] data, int offset, int size)
        {
            uint previous = unAcknowledge;
            uint maxAcknowledge = 0;
            int flag = 0;

            if (data == null || size < OVERHEAD)
            {
                return -1;
            }

            while (true)
            {
                if (size < OVERHEAD) //数据包大小至少有一个头部的大小
                {
                    break;
                }
                
                offset += Helper.Decode32U(data, offset, out uint conv);
                if (conv != conversation) return -1;
                offset += Helper.Decode8u(data, offset, out byte command);
                offset += Helper.Decode8u(data, offset, out byte fragment);
                offset += Helper.Decode16U(data, offset, out ushort windowSize);
                offset += Helper.Decode32U(data, offset, out uint timestamp);
                offset += Helper.Decode32U(data, offset, out uint serialNumber);
                offset += Helper.Decode32U(data, offset, out uint newUnAcknowledge);
                offset += Helper.Decode32U(data, offset, out uint length);
                size -= OVERHEAD;// 减去头部大小
                
                if (size < length)
                {
                    return -2;
                }
                
                if (command != CMD_PUSH && command != CMD_ACK && command != CMD_WIN_ASK && command != CMD_WIN_INS)
                {
                    return -3;
                }

                remoteWindowSize = windowSize;
                ParseUnAcknowledge(newUnAcknowledge);
                ShrinkBuffer();

                switch (command)
                {
                    case CMD_ACK: // RTT 相关的信息，并解析序列号
                        if (Subtract(currentTime, timestamp) >= 0)
                        {
                            UpdateAcknowledge(Subtract(currentTime, timestamp));
                        }
                        ParseAcknowledge(serialNumber);
                        ShrinkBuffer();
                        if (flag == 0)
                        {
                            flag = 1;
                            maxAcknowledge = serialNumber;
                        }
                        else
                        {
                            if (Subtract(serialNumber, maxAcknowledge) > 0)
                            {
                                maxAcknowledge = serialNumber;
                            }
                        }

                        break;
                    case CMD_PUSH:// 分片的序列号在接收窗口内，则进行确认并将分片添加到接收缓冲区，然后解析数据分片
                        if (Subtract(serialNumber, receiveNextSegment + receiveWindowSize) < 0)
                        {
                            AcknowledgePush(serialNumber, timestamp);
                            if (Subtract(serialNumber, receiveNextSegment) >= 0)
                            {
                                Segment seg = segmentPool.Pop();
                                seg.conversation = conv;
                                seg.command = command;
                                seg.fragment = fragment;
                                seg.windowSize = windowSize;
                                seg.timestamp  = timestamp;
                                seg.serialNumber  = serialNumber;
                                seg.unAcknowledge = newUnAcknowledge;
                                if (length > 0)
                                {
                                    seg.stream.Write(data, offset, (int)length);
                                }
                                ParseSegment(seg);
                            }
                        }

                        break;
                    case CMD_WIN_ASK: // 收到远端的窗口探测请求，表示对方想知道本地窗口大小
                        probe |= ASK_TELL;
                        break;
                }

                offset += (int)length;
                size -= (int)length;
            }

            if (flag != 0)
            {
                ParseFastAcknowledge(maxAcknowledge);
            }

            // 根据需要进行拥塞窗口的更新
            if (Subtract(unAcknowledge, previous) > 0)
            {
                if (congestionWindowSize < remoteWindowSize)
                {
                    if (congestionWindowSize < slowStartThreshold)
                    {
                        congestionWindowSize++;
                        increment += maxSegmentSize;
                    }
                    else
                    {
                        if (increment < maxSegmentSize) increment = maxSegmentSize;
                        increment += (maxSegmentSize * maxSegmentSize) / increment + (maxSegmentSize / 16);
                        if ((congestionWindowSize + 1) * maxSegmentSize <= increment)
                        {
                            congestionWindowSize = (increment + maxSegmentSize - 1) / ((maxSegmentSize > 0) ? maxSegmentSize : 1);
                        }
                    }
                    if (congestionWindowSize > remoteWindowSize)
                    {
                        congestionWindowSize = remoteWindowSize;
                        increment = remoteWindowSize * maxSegmentSize;
                    }
                }
            }

            return 0;
        }

        public void Flush()
        {
            // 在刷新之前需要调用Update
            if (!updated) return;
            
            int size  = 0; // 要刷新的字节大小
            bool lost = false; // 是否有丢失的片段
            
            Segment seg = segmentPool.Pop();
            seg.conversation = conversation;
            seg.command = CMD_ACK;
            seg.windowSize = WindowUnused();
            seg.unAcknowledge = receiveNextSegment;

            // 更新确认
            foreach (var packet in packetList)
            {
                MakeSpace(ref size, OVERHEAD);
                seg.serialNumber = packet.serialNumber;
                seg.timestamp = packet.timestamp;
                size += seg.Encode(buffer, size);
            }
            packetList.Clear();
            
            ProbeUpdate(); // 探测窗口大小(如果远程窗口大小等于零)

            if ((probe & ASK_SEND) != 0) // 刷新窗口探测命令
            {
                seg.command = CMD_WIN_ASK;
                MakeSpace(ref size, OVERHEAD);
                size += seg.Encode(buffer, size);
            }

            if ((probe & ASK_TELL) != 0) // 刷新窗口探测命令
            {
                seg.command = CMD_WIN_INS;
                MakeSpace(ref size, OVERHEAD);
                size += seg.Encode(buffer, size);
            }

            probe = 0;

            // 计算当前可以安全发送的窗口大小
            uint congestionWindow = Math.Min(sendWindowSize, remoteWindowSize);
            
            if (!noCongestionWindow) // 如果拥塞窗口启用，则限制窗口大小 congestionWindow
            {
                congestionWindow = Math.Min(congestionWindowSize, congestionWindow);
            }

            // 移动拥塞窗口的消息 从 sendQueue 到 sendBuffers
            while (Subtract(sendNextSegment, unAcknowledge + congestionWindow) < 0)
            {
                if (sendQueue.Count == 0) break;
                var segment = sendQueue.Dequeue();
                segment.conversation = conversation;
                segment.command = CMD_PUSH;
                segment.windowSize = seg.windowSize;
                segment.timestamp = currentTime;
                segment.serialNumber = sendNextSegment;
                sendNextSegment += 1; // 增加下一段的序号
                segment.unAcknowledge = receiveNextSegment;
                segment.resendTimestamp = currentTime;
                segment.retransmitTimeout = receiveRto;
                segment.fastAcknowledge = 0;
                segment.retransmitCount = 0;
                sendBuffers.Add(segment);
            }

            //计算重新发送
            uint resent = fastResend > 0 ? (uint)fastResend : 0xffffffff;
            uint minRto = noDelay == 0 ? (uint)receiveRto >> 3 : 0;
            
            int change = 0; // 刷新数据分段
            foreach (Segment segment in sendBuffers)
            {
                bool needSend = false;
                if (segment.retransmitCount == 0) // 初始化传输
                {
                    needSend = true;
                    segment.retransmitCount++;
                    segment.retransmitTimeout = receiveRto;
                    segment.resendTimestamp = currentTime + (uint)segment.retransmitTimeout + minRto;
                }
                else if (Subtract(currentTime, segment.resendTimestamp) >= 0) // 重传超时RTO
                {
                    needSend = true;
                    segment.retransmitCount++;
                    if (noDelay == 0)
                    {
                        segment.retransmitTimeout += Math.Max(segment.retransmitTimeout, receiveRto);
                    }
                    else
                    {
                        int step = noDelay < 2 ? segment.retransmitTimeout : receiveRto;
                        segment.retransmitTimeout += step / 2;
                    }
                    segment.resendTimestamp = currentTime + (uint)segment.retransmitTimeout;
                    lost = true;
                }
                else if (segment.fastAcknowledge >= resent) // 快速重传确认
                {
                    if (segment.retransmitCount <= fastLimit || fastLimit <= 0)
                    {
                        needSend = true;
                        segment.retransmitCount++;
                        segment.fastAcknowledge = 0;
                        segment.resendTimestamp = currentTime + (uint)segment.retransmitTimeout;
                        change++;
                    }
                }

                if (needSend)
                {
                    segment.timestamp = currentTime;
                    segment.windowSize = seg.windowSize;
                    segment.unAcknowledge = receiveNextSegment;
                    int need = OVERHEAD + (int)segment.stream.Position;
                    MakeSpace(ref size, need);

                    size += segment.Encode(buffer, size);

                    if (segment.stream.Position > 0)
                    {
                        Buffer.BlockCopy(segment.stream.GetBuffer(), 0, buffer, size, (int)segment.stream.Position);
                        size += (int)segment.stream.Position;
                    }
                    
                    if (segment.retransmitCount >= deadLink)
                    {
                        state = -1; // 如果消息被重发N次，则发生死链接
                    }
                }
            }
            
            segmentPool.Push(seg);
            FlushBuffer(size); // 刷新剩余的Buffer
            
            if (change > 0)//更新 慢启动阈值
            {
                uint inflight = sendNextSegment - unAcknowledge;
                slowStartThreshold = inflight / 2;
                if (slowStartThreshold < THRESH_MIN)
                {
                    slowStartThreshold = THRESH_MIN;
                }

                congestionWindowSize = slowStartThreshold + resent;
                increment = congestionWindowSize * maxSegmentSize;
            }
            
            if (lost)
            {
                slowStartThreshold = congestionWindow / 2;
                if (slowStartThreshold < THRESH_MIN)
                {
                    slowStartThreshold = THRESH_MIN;
                }

                congestionWindowSize = 1;
                increment = maxSegmentSize;
            }

            if (congestionWindowSize < 1)
            {
                congestionWindowSize = 1;
                increment = maxSegmentSize;
            }
            
            uint WindowUnused()
            {
                return receiveQueue.Count < receiveWindowSize ? receiveWindowSize - (uint)receiveQueue.Count : 0;
            }
            
            void MakeSpace(ref int localSize, int space)
            {
                if (localSize + space > maxTransferUnit)
                {
                    onOutput(buffer, localSize);
                    localSize = 0;
                }
            }
        
            void FlushBuffer(int localSize)
            {
                if (localSize > 0)
                {
                    onOutput(buffer, localSize);
                }
            }

            void ProbeUpdate()
            {
                if (remoteWindowSize == 0)
                {
                    if (probeWaitTime == 0)
                    {
                        probeWaitTime = PROBE_INIT;
                        probeTimestamp = currentTime + probeWaitTime;
                    }
                    else
                    {
                        if (Subtract(currentTime, probeTimestamp) >= 0)
                        {
                            if (probeWaitTime < PROBE_INIT)
                            {
                                probeWaitTime = PROBE_INIT;
                            }

                            probeWaitTime += probeWaitTime / 2;
                            if (probeWaitTime > PROBE_LIMIT)
                            {
                                probeWaitTime = PROBE_LIMIT;
                            }

                            probeTimestamp = currentTime + probeWaitTime;
                            probe |= ASK_SEND;
                        }
                    }
                }
                else
                {
                    probeTimestamp = 0;
                    probeWaitTime = 0;
                }
            }
        }

        public void Update(long currentTime)
        {
            this.currentTime = (uint)currentTime;

            if (!updated)
            {
                updated = true;
                timestampFlush = this.currentTime; //当前时间戳，表示最后一次刷新时间
            }

            int slap = Subtract(this.currentTime, timestampFlush); //计算距离上次刷新的时间间隔

            if (Math.Abs(slap) > 10000)
            {
                timestampFlush = this.currentTime;
                slap = 0;
            }

            if (slap >= 0)
            {
                timestampFlush += interval;

                if (this.currentTime >= timestampFlush)
                {
                    timestampFlush = this.currentTime + interval;
                }

                Flush();
            }
        }

        public void SetTransferUnit(uint maxTransferUnit)
        {
            if (maxTransferUnit is < 50 or < OVERHEAD)
            {
                throw new ArgumentException("MTU must be higher than 50 and higher than OVERHEAD");
            }

            buffer = new byte[(maxTransferUnit + OVERHEAD) * 3];
            this.maxTransferUnit = maxTransferUnit;
            maxSegmentSize = maxTransferUnit - OVERHEAD;
        }

        public void SetNoDelay(uint noDelay, uint interval = INTERVAL, int resend = 0, bool congestion = false)
        {
            this.noDelay = noDelay;
            receiveMinRto = noDelay != 0 ? RTO_NDL : RTO_MIN;
            this.interval = Math.Clamp(interval, 10, 5000);

            if (resend >= 0)
            {
                fastResend = resend;
            }

            noCongestionWindow = congestion;
        }

        public void SetWindowSize(uint sendWindow, uint receiveWindow)
        {
            if (sendWindow > 0)
            {
                sendWindowSize = sendWindow;
            }

            if (receiveWindow > 0)
            {
                receiveWindowSize = Math.Max(receiveWindow, WIN_RCV);
            }
        }

        public int GetBufferQueueCount()
        {
            return receiveQueue.Count + sendQueue.Count + receiveBuffers.Count + sendBuffers.Count;
        }

        private static int Subtract(uint later, uint earlier)
        {
            return (int)(later - earlier);
        }
    }
}