using System;
using System.Collections.Generic;
using System.Linq;

namespace JFramework.Udp
{
    internal sealed class Protocol
    {
        private struct Packet
        {
            public uint sendId;
            public uint sendTime;
        }
        
        public const int FRG_MAX = 255;            // 分片的最大数量，使用1字节进行编码，最大为255
        public const int WIN_SND = 32;             // 默认的发送窗口大小
        public const int WIN_RCV = 128;            // 默认的接收窗口大小，必须大于等于最大分片大小
        public const int MTU_DEF = 1200;           // 默认的最大传输单元MTU
        public const int OVERHEAD = 24;            // 数据包头部的额外开销
        public const int INTERVAL = 100;           // 内部处理时钟的间隔时间
        private const int RTO_MIN = 30;            // 无延迟的最小重传超时时间
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
        public int state;                          // Udp状态
        
        private int rtt;                           // 平滑的往返时间（RTT）的加权平均值
        private int rto;                           // 接收方的RTO
        private int shake;                         // RTT的平均偏差，用于衡量RTT的抖动
        private uint resend;                       // 快速重传的触发次数
        private uint maxUnit;                      // 最大传输单元
        private uint serialId;                     // 未确认的序号，例为9表示8已经被确认，9和10已经发送。
        private uint nextSendId;                   // 发送数据的下一个序号，不断增加
        private uint nextSegment;                  // 接收数据的下一个分段，不断增加
        private uint segmentSize;                  // 最大分段大小
        private uint threshold;                    // 慢启动阈值
        private uint sendWindow;                   // 发送窗口大小
        private uint receiveWindow;                // 接收窗口大小
        private uint remoteWindow;                 // 远端窗口大小
        private uint congestWindow;                // 拥塞窗口大小
        private uint probe;                        // 探测标志
        private uint interval;                     // 内部处理时钟的间隔时间
        private uint flushTime;                    // 最后一次刷新数据的时间戳（毫秒）
        private bool updated;                      // 是否更新
        private uint probeTimestamp;               // 探测窗口的时间戳
        private uint probeWaitTime;                // 等待探测的时间
        private uint increment;                    // 拥塞窗口增加的步进值
        private uint lateTime;                     // 当前时间（毫秒），由Update方法设置
        private byte[] buffer;                     // 接收缓存
        public readonly uint deadLink;             // 一个分段被认为丢失之前的最大重传次数
        private readonly int fastLimit;            // 快速重传的最大限制次数
        private readonly uint conversation;        // 会话Id
        private readonly Action<byte[], int> onOutput;
        private readonly Pool pool = new Pool(32);
        private readonly List<Packet> packetList = new List<Packet>(16);
        private readonly List<Segment> sendBuffers = new List<Segment>(16);
        private readonly List<Segment> receiveBuffers = new List<Segment>(16);
        public readonly Queue<Segment> sendQueue = new Queue<Segment>(16); 
        private readonly Queue<Segment> receiveQueue = new Queue<Segment>(16);
        
        public Protocol(Action<byte[], int> onOutput)
        {
            this.onOutput = onOutput;
            sendWindow = WIN_SND;
            receiveWindow = WIN_RCV;
            remoteWindow = WIN_RCV;
            segmentSize = maxUnit - OVERHEAD;
            rto = RTO_DEF;
            interval = INTERVAL;
            flushTime = INTERVAL;
            threshold = THRESH_INIT;
            fastLimit = FAST_ACK_LIMIT;
            deadLink = DEAD_LINK;
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="buffer">传入的数据</param>
        /// <returns>返回读取的字节数</returns>
        public int Receive(byte[] buffer)
        {
            var recover = receiveQueue.Count >= receiveWindow;
            var offset = 0;
            var length = 0;
            while (receiveQueue.Count > 0) // 先从接收队列中取出数据段，然后将数据从每个段中复制出来
            {
                var segment = receiveQueue.Dequeue();
                var position = (int)segment.stream.Position;
                Buffer.BlockCopy(segment.stream.GetBuffer(), 0, buffer, offset, position);
                offset += position;
                length += position;
                var fragment = segment.fragment;
                pool.Push(segment);
                if (fragment == 0)
                {
                    break;
                }
            }

            var removed = 0;
            foreach (var segment in receiveBuffers) // 再接着循环处理存有待接收数据段的buffer
            {
                if (segment.sendId == nextSegment && receiveQueue.Count < receiveWindow)
                {
                    removed++;
                    receiveQueue.Enqueue(segment);
                    nextSegment++;
                }
                else
                {
                    break;
                }
            }

            receiveBuffers.RemoveRange(0, removed);
            if (receiveQueue.Count < receiveWindow && recover)
            {
                probe |= ASK_TELL; // 对于丢失或者乱序的数据包，我们可能需要通过一些恢复策略来请求序列号重新排列
            }

            return length;
        }

        public int GetLength()
        {
            var length = 0;
            if (receiveQueue.Count == 0)
            {
                return -1; //队列为空
            }

            var segment = receiveQueue.Peek();
            if (segment.fragment == 0)
            {
                return (int)segment.stream.Position; // 表示消息不需要分片，此段的大小就是消息的最终大小，将其作为返回值。
            }

            if (receiveQueue.Count < segment.fragment + 1)
            {
                return -1; // 有分片未接收完整
            }

            foreach (var receive in receiveQueue) // 累加分片长度
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
            int count;
            if (length > segmentSize)
            {
                count = (int)((length - 1) / segmentSize) + 1;
            }
            else
            {
                count = 1;
            }

            if (count > FRG_MAX)
            {
                return -1;
            }

            if (count >= receiveWindow)
            {
                return -2; // 不能大于接收窗口的大小
            }

            for (int i = 0; i < count; i++) // 写入分段数量
            {
                var size = length > segmentSize ? (int)segmentSize : length;
                var segment = pool.Pop();

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
        /// <param name="time"></param>
        private void UpdateRto(int time)
        {
            if (rtt == 0)
            {
                rtt = time;
                shake = time / 2;
            }
            else
            {
                int abs = Math.Abs(time - rtt);
                shake = (3 * shake + abs) / 4;
                rtt = (7 * rtt + time) / 8;
                if (rtt < 1)
                {
                    rtt = 1;
                }
            }

            int newRto = rtt + Math.Max((int)interval, 4 * shake);
            rto = Math.Clamp(newRto, RTO_MIN, RTO_MAX);
        }

        private void InputSendBuffer(uint sendId)
        {
            if (Utility.Subtract(sendId, serialId) < 0)
            {
                return;
            }

            if (Utility.Subtract(sendId, nextSendId) >= 0)
            {
                return;
            }

            for (int i = 0; i < sendBuffers.Count; ++i)
            {
                var segment = sendBuffers[i];
                if (segment.sendId == sendId)
                {
                    sendBuffers.RemoveAt(i);
                    pool.Push(segment);
                    break;
                }

                if (Utility.Subtract(sendId, segment.sendId) < 0)
                {
                    break;
                }
            }
        }

        private void ParseFastAcknowledge(uint serialNumber)
        {
            if (serialNumber < serialId)
            {
                return;
            }

            if (serialNumber >= nextSendId)
            {
                return;
            }

            foreach (var segment in sendBuffers.TakeWhile(segment => serialNumber >= segment.sendId).Where(segment => serialNumber != segment.sendId))
            {
                segment.resendId++;
            }
        }

        private void AcknowledgePush(uint serialNumber, uint timestamp)
        {
            packetList.Add(new Packet
            {
                sendId = serialNumber, 
                sendTime = timestamp
            });
        }

        private void ParseSegment(Segment segment)
        {
            uint serialNumber = segment.sendId;
            if (Utility.Subtract(serialNumber, nextSegment + receiveWindow) >= 0 || Utility.Subtract(serialNumber, nextSegment) < 0)
            {
                pool.Push(segment);
                return;
            }

            InsertSegmentInReceiveBuffer(segment);
            MoveBufferToQueue();
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
                if (receive.sendId == segment.sendId)
                {
                    repeat = true; // 找到了有相同序列号的分片
                    break;
                }

                if (Utility.Subtract(segment.sendId, receive.sendId) > 0)
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
                pool.Push(segment); // 重复则进行回收
            }
        }
        
        /// <summary>
        /// 移动分段从 receiveBuffers 到 receiveQueue
        /// </summary>
        private void MoveBufferToQueue()
        {
            var removed = 0;
            foreach (var segment in receiveBuffers)
            {
                if (segment.sendId == nextSegment && receiveQueue.Count < receiveWindow)
                {
                    removed++;
                    receiveQueue.Enqueue(segment);
                    nextSegment++;
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
            uint previous = serialId;
            uint confirm = 0;
            int isFlag = 0;

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

                offset += Utility.Decode32U(data, offset, out var id);
                if (id != conversation) return -1;
                offset += Utility.Decode8U(data, offset, out var command);
                offset += Utility.Decode8U(data, offset, out var fragment);
                offset += Utility.Decode16U(data, offset, out var window);
                offset += Utility.Decode32U(data, offset, out var sendTime);
                offset += Utility.Decode32U(data, offset, out var sendId);
                offset += Utility.Decode32U(data, offset, out var receiveId);
                offset += Utility.Decode32U(data, offset, out var length);
                size -= OVERHEAD; // 减去头部大小

                if (size < length)
                {
                    return -2;
                }

                if (command != CMD_PUSH && command != CMD_ACK && command != CMD_WIN_ASK && command != CMD_WIN_INS)
                {
                    return -3;
                }

                remoteWindow = window;

                var removed = 0;
                foreach (var segment in sendBuffers)
                {
                    if (segment.sendId < receiveId)
                    {
                        removed++;
                        pool.Push(segment);
                    }
                    else
                    {
                        break;
                    }
                }

                sendBuffers.RemoveRange(0, removed);
                serialId = sendBuffers.Count > 0 ? sendBuffers[0].sendId : nextSendId;

                switch (command)
                {
                    case CMD_ACK: // RTT 相关的信息，并解析序列号
                        if (Utility.Subtract(lateTime, sendTime) >= 0)
                        {
                            UpdateRto(Utility.Subtract(lateTime, sendTime));
                        }

                        InputSendBuffer(sendId);
                        serialId = sendBuffers.Count > 0 ? sendBuffers[0].sendId : nextSendId;
                        if (isFlag == 0)
                        {
                            isFlag = 1;
                            confirm = sendId;
                        }
                        else if (Utility.Subtract(sendId, confirm) > 0)
                        {
                            confirm = sendId;
                        }

                        break;
                    case CMD_PUSH: // 分片的序列号在接收窗口内，则进行确认并将分片添加到接收缓冲区，然后解析数据分片
                        if (Utility.Subtract(sendId, nextSegment + receiveWindow) < 0)
                        {
                            AcknowledgePush(sendId, sendTime);
                            if (Utility.Subtract(sendId, nextSegment) >= 0)
                            {
                                var segment = pool.Pop();
                                segment.id = id;
                                segment.command = command;
                                segment.fragment = fragment;
                                segment.window = window;
                                segment.sendTime = sendTime;
                                segment.sendId = sendId;
                                segment.receiveId = receiveId;
                                if (length > 0)
                                {
                                    segment.stream.Write(data, offset, (int)length);
                                }

                                ParseSegment(segment);
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

            if (isFlag != 0)
            {
                ParseFastAcknowledge(confirm);
            }

            // 根据需要进行拥塞窗口的更新
            if (Utility.Subtract(serialId, previous) > 0)
            {
                if (congestWindow < remoteWindow)
                {
                    if (congestWindow < threshold)
                    {
                        congestWindow++;
                        increment += segmentSize;
                    }
                    else
                    {
                        if (increment < segmentSize) increment = segmentSize;
                        increment += (segmentSize * segmentSize) / increment + (segmentSize / 16);
                        if ((congestWindow + 1) * segmentSize <= increment)
                        {
                            congestWindow = (increment + segmentSize - 1) / ((segmentSize > 0) ? segmentSize : 1);
                        }
                    }

                    if (congestWindow > remoteWindow)
                    {
                        congestWindow = remoteWindow;
                        increment = remoteWindow * segmentSize;
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
            
            Segment seg = pool.Pop();
            seg.id = conversation;
            seg.command = CMD_ACK;
            seg.window = WindowUnused();
            seg.receiveId = nextSegment;

            // 更新确认
            foreach (var packet in packetList)
            {
                MakeSpace(ref size, OVERHEAD);
                seg.sendId = packet.sendId;
                seg.sendTime = packet.sendTime;
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
            var windowSize = Math.Min(sendWindow, remoteWindow);

            // 移动拥塞窗口的消息 从 sendQueue 到 sendBuffers
            while (Utility.Subtract(nextSendId, serialId + windowSize) < 0)
            {
                if (sendQueue.Count == 0) break;
                var segment = sendQueue.Dequeue();
                segment.id = conversation;
                segment.command = CMD_PUSH;
                segment.window = seg.window;
                segment.sendTime = lateTime;
                segment.sendId = nextSendId;
                nextSendId += 1; // 增加下一段的序号
                segment.receiveId = nextSegment;
                segment.resendTime = lateTime;
                segment.failure = rto;
                segment.resendId = 0;
                segment.resendCount = 0;
                sendBuffers.Add(segment);
            }
            
            int change = 0; // 刷新数据分段
            foreach (Segment segment in sendBuffers)
            {
                bool needSend = false;
                if (segment.resendCount == 0) // 初始化传输
                {
                    needSend = true;
                    segment.resendCount++;
                    segment.failure = rto;
                    segment.resendTime = lateTime + (uint)segment.failure;
                }
                else if (Utility.Subtract(lateTime, segment.resendTime) >= 0) // 重传超时RTO
                {
                    needSend = true;
                    segment.resendCount++;
                    segment.failure += segment.failure / 2;
                    segment.resendTime = lateTime + (uint)segment.failure;
                    lost = true;
                }
                else if (segment.resendId >= resend) // 快速重传确认
                {
                    if (segment.resendCount <= fastLimit || fastLimit <= 0)
                    {
                        needSend = true;
                        segment.resendCount++;
                        segment.resendId = 0;
                        segment.resendTime = lateTime + (uint)segment.failure;
                        change++;
                    }
                }

                if (needSend)
                {
                    segment.sendTime = lateTime;
                    segment.window = seg.window;
                    segment.receiveId = nextSegment;
                    int need = OVERHEAD + (int)segment.stream.Position;
                    MakeSpace(ref size, need);

                    size += segment.Encode(buffer, size);

                    if (segment.stream.Position > 0)
                    {
                        Buffer.BlockCopy(segment.stream.GetBuffer(), 0, buffer, size, (int)segment.stream.Position);
                        size += (int)segment.stream.Position;
                    }
                    
                    if (segment.resendCount >= deadLink)
                    {
                        state = -1; // 如果消息被重发N次，则发生死链接
                    }
                }
            }
            
            pool.Push(seg);
            FlushBuffer(size); // 刷新剩余的Buffer
            
            if (change > 0)//更新 慢启动阈值
            {
                uint inflight = nextSendId - serialId;
                threshold = inflight / 2;
                if (threshold < THRESH_MIN)
                {
                    threshold = THRESH_MIN;
                }

                congestWindow = threshold + resend;
                increment = congestWindow * segmentSize;
            }
            
            if (lost)
            {
                threshold = windowSize / 2;
                if (threshold < THRESH_MIN)
                {
                    threshold = THRESH_MIN;
                }

                congestWindow = 1;
                increment = segmentSize;
            }

            if (congestWindow < 1)
            {
                congestWindow = 1;
                increment = segmentSize;
            }
            
            uint WindowUnused()
            {
                return receiveQueue.Count < receiveWindow ? receiveWindow - (uint)receiveQueue.Count : 0;
            }
            
            void MakeSpace(ref int localSize, int space)
            {
                if (localSize + space > maxUnit)
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
                if (remoteWindow == 0)
                {
                    if (probeWaitTime == 0)
                    {
                        probeWaitTime = PROBE_INIT;
                        probeTimestamp = lateTime + probeWaitTime;
                    }
                    else
                    {
                        if (Utility.Subtract(lateTime, probeTimestamp) >= 0)
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

                            probeTimestamp = lateTime + probeWaitTime;
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
            lateTime = (uint)currentTime;

            if (!updated)
            {
                updated = true;
                flushTime = lateTime; //当前时间戳，表示最后一次刷新时间
            }

            int slap = Utility.Subtract(lateTime, flushTime); //计算距离上次刷新的时间间隔

            if (Math.Abs(slap) > 10000)
            {
                flushTime = lateTime;
                slap = 0;
            }

            if (slap >= 0)
            {
                flushTime += interval;

                if (lateTime >= flushTime)
                {
                    flushTime = lateTime + interval;
                }

                Flush();
            }
        }

        public void SetUnit(uint maxUnit)
        {
            this.maxUnit = Math.Max(maxUnit, 50);
            buffer = new byte[(maxUnit + OVERHEAD) * 3];
            segmentSize = maxUnit - OVERHEAD;
        }

        public void SetResend(uint interval, uint resend)
        {
            this.resend = resend;
            this.interval = Math.Clamp(interval, 10, 5000);
        }

        public void SetWindow(uint sendWindow, uint receiveWindow)
        {
            if (sendWindow > 0)
            {
                this.sendWindow = sendWindow;
            }

            if (receiveWindow > 0)
            {
                this.receiveWindow = Math.Max(receiveWindow, WIN_RCV);
            }
        }

        public int GetBufferQueueCount()
        {
            return receiveQueue.Count + sendQueue.Count + receiveBuffers.Count + sendBuffers.Count;
        }
    }
}