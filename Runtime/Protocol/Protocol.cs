using System;
using System.Collections.Generic;
using System.Linq;

namespace JFramework.Udp
{
    internal sealed class Protocol
    {
        public const int WIN_SED = 32;          // 默认的发送窗口大小
        public const int WIN_REV = 128;         // 默认的接收窗口大小，必须大于等于最大分片大小
        public const int MTU_MAX = 1200;        // 默认的最大传输单元MTU
        public const int HEAD = 24;             // 数据包头部的额外开销
        public const int TIME = 100;            // 内部处理时钟的间隔时间
        public const int DEAD = 20;             // 当一个段被认为丢失之前的最大重传次数
        public const int RESEND = 5;            // 触发快速重传的最大次数
        public const int TIMEOUT = 10000;       // 超时时间
        private const int RTO_MIN = 30;         // 无延迟的最小重传超时时间
        private const int RTO_DEF = 200;        // 默认RTO
        private const int RTO_MAX = 60000;      // 最大RTO
        private const int CMD_PUSH = 81;        // 推送数据命令
        private const int CMD_ACK = 82;         // 确认收到数据命令
        private const int CMD_WIN_ASK = 83;     // 窗口探测请求命令
        private const int CMD_WIN_INS = 84;     // 窗口大小插入命令
        private const int ASK_SEND = 1;         // 需要发送的 CMD_WIN_ASK 命令
        private const int ASK_TELL = 2;         // 需要发送的 CMD_WIN_INS 命令
        private const int THRESH_DEF = 2;       // 拥塞窗口增长阈值的初始值
        private const int THRESH_MIN = 2;       // 拥塞窗口增长阈值的最小值
        private const int PROBE_DEF = 7000;     // 探测窗口大小的初始时间（7秒）
        private const int PROBE_MAX = 120000;   // 探测窗口大小的最长时间（120秒）
        private const int QUEUE_COUNT = 10000;  // 网络缓存数量
        public int state;                       // Udp状态
        private int rtt;                        // 平滑的往返时间（RTT）的加权平均值
        private int rto;                        // 接收方的RTO
        private int shake;                      // RTT的平均偏差，用于衡量RTT的抖动
        private uint resend;                    // 快速重传的触发次数
        private uint maxUnit;                   // 最大传输单元
        private uint serialId;                  // 未确认的序号，例为9表示8已经被确认，9和10已经发送。
        private uint nextSendId;                // 发送数据的下一个序号，不断增加
        private uint nextReceiveId;             // 接收数据的下一个分段，不断增加
        private uint segmentSize;               // 最大分段大小
        private uint threshold;                 // 慢启动阈值
        private uint sendWindow;                // 发送窗口大小
        private uint receiveWindow;             // 接收窗口大小
        private uint remoteWindow;              // 远端窗口大小
        private uint congestWindow;             // 拥塞窗口大小
        private uint interval;                  // 内部处理时钟的间隔时间
        private bool updated;                   // 是否更新
        private uint refreshTime;               // 最后一次刷新数据的时间戳（毫秒）
        private uint probe;                     // 探测标志
        private uint probeTime;                 // 探测窗口的时间戳
        private uint probeWait;                // 等待探测的时间
        private uint increment;                 // 拥塞窗口增加的步进值
        private uint sinceTime;                 // 当前时间（毫秒），由Update方法设置
        private byte[] buffer;                  // 接收缓存
        private readonly uint current;          // 当前会话Id
        private readonly uint resendLimit;      // 快速重传的最大限制次数
        private readonly Pool pool = new Pool(32);
        private readonly List<Message> messages = new List<Message>(16);
        private readonly List<Segment> sends = new List<Segment>(16);
        private readonly List<Segment> receives = new List<Segment>(16);
        private readonly Queue<Segment> sendQueue = new Queue<Segment>(16);
        private readonly Queue<Segment> receiveQueue = new Queue<Segment>(16);
        private event Action<byte[], int> onRefresh;

        public Protocol(uint current, Action<byte[], int> onRefresh)
        {
            rto = RTO_DEF;
            threshold = THRESH_DEF;
            refreshTime = TIME;
            resendLimit = RESEND;
            remoteWindow = WIN_REV;
            this.current = current;
            this.onRefresh = onRefresh;
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="buffer">传入的数据</param>
        /// <returns>返回读取的字节数</returns>
        public int Receive(byte[] buffer)
        {
            var offset = 0;
            var length = 0;
            var recover = receiveQueue.Count >= receiveWindow;
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
            foreach (var segment in receives) // 再接着循环处理存有待接收数据段的buffer
            {
                if (segment.sendId == nextReceiveId && receiveQueue.Count < receiveWindow)
                {
                    removed++;
                    receiveQueue.Enqueue(segment);
                    nextReceiveId++;
                }
                else
                {
                    break;
                }
            }

            receives.RemoveRange(0, removed);
            if (receiveQueue.Count < receiveWindow && recover)
            {
                probe |= ASK_TELL; // 对于丢失或者乱序的数据包，我们可能需要通过一些恢复策略来请求序列号重新排列
            }

            return length;
        }

        /// <summary>
        /// 获取分段长度
        /// </summary>
        /// <returns></returns>
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

            if (count > byte.MaxValue) // 分片数量不能大于 1 字节
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
        private void UpdateRTO(int time)
        {
            if (rtt == 0)
            {
                rtt = time;
                shake = time / 2;
            }
            else
            {
                shake = (3 * shake + Math.Abs(time - rtt)) / 4;
                rtt = (7 * rtt + time) / 8;
                if (rtt < 1)
                {
                    rtt = 1;
                }
            }

            int newRto = rtt + Math.Max((int)interval, 4 * shake);
            rto = Math.Clamp(newRto, RTO_MIN, RTO_MAX);
        }

        private void UpdateSend(uint sendId)
        {
            if (Utility.Compare(sendId, serialId) < 0)
            {
                return;
            }

            if (Utility.Compare(sendId, nextSendId) >= 0)
            {
                return;
            }

            for (int i = 0; i < sends.Count; ++i)
            {
                var segment = sends[i];
                if (segment.sendId == sendId)
                {
                    sends.RemoveAt(i);
                    pool.Push(segment);
                    break;
                }

                if (Utility.Compare(sendId, segment.sendId) < 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 设置重传
        /// </summary>
        /// <param name="sendId"></param>
        private void Resend(uint sendId)
        {
            if (sendId < serialId)
            {
                return;
            }

            if (sendId >= nextSendId)
            {
                return;
            }

            foreach (var segment in sends.TakeWhile(segment => sendId >= segment.sendId).Where(segment => sendId != segment.sendId))
            {
                segment.resendId++;
            }
        }

        /// <summary>
        /// 按照分片的序列号有序地插入接收缓冲区中
        /// </summary>
        /// <param name="segment">插入的分片</param>
        private void Insert(Segment segment)
        {
            var sendId = segment.sendId;
            if (Utility.Compare(sendId, nextReceiveId + receiveWindow) >= 0)
            {
                pool.Push(segment);
                return;
            }

            if (Utility.Compare(sendId, nextReceiveId) < 0)
            {
                pool.Push(segment);
                return;
            }

            int index;
            var isFind = false;
            for (index = receives.Count - 1; index >= 0; index--)
            {
                var receive = receives[index];
                if (receive.sendId == segment.sendId)
                {
                    isFind = true; // 找到了有相同序列号的分片
                    break;
                }

                if (Utility.Compare(segment.sendId, receive.sendId) > 0)
                {
                    break;
                }
            }

            if (!isFind)
            {
                receives.Insert(index + 1, segment); // 未找到则执行插入
            }
            else
            {
                pool.Push(segment); // 重复则进行回收
            }

            var removed = 0;
            foreach (var receive in receives)
            {
                if (receive.sendId == nextReceiveId && receiveQueue.Count < receiveWindow)
                {
                    removed++;
                    receiveQueue.Enqueue(receive);
                    nextReceiveId++;
                }
                else
                {
                    break;
                }
            }

            receives.RemoveRange(0, removed);
        }

        /// <summary>
        /// 当接收到底层数据包(例如UDP数据包)时使用
        /// </summary>
        public int Input(byte[] data, int offset, int size)
        {
            if (data == null || size < HEAD)
            {
                return -1;
            }

            var previous = serialId;
            var isConfirm = false;
            uint confirmId = 0;
            while (true)
            {
                if (size < HEAD) //数据包大小至少有一个头部的大小
                {
                    break;
                }

                offset += Utility.Decode32U(data, offset, out var id);
                if (current != id) return -1;
                offset += Utility.Decode8U(data, offset, out var command);
                offset += Utility.Decode8U(data, offset, out var fragment);
                offset += Utility.Decode16U(data, offset, out var window);
                offset += Utility.Decode32U(data, offset, out var sendTime);
                offset += Utility.Decode32U(data, offset, out var sendId);
                offset += Utility.Decode32U(data, offset, out var receiveId);
                offset += Utility.Decode32U(data, offset, out var length);
                size -= HEAD; // 减去头部大小

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
                foreach (var segment in sends)
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

                sends.RemoveRange(0, removed);
                serialId = sends.Count > 0 ? sends[0].sendId : nextSendId;
                switch (command)
                {
                    case CMD_ACK: // RTT 相关的信息，并解析序列号
                        if (Utility.Compare(sinceTime, sendTime) >= 0)
                        {
                            UpdateRTO(Utility.Compare(sinceTime, sendTime));
                        }

                        UpdateSend(sendId);
                        serialId = sends.Count > 0 ? sends[0].sendId : nextSendId;
                        if (!isConfirm)
                        {
                            isConfirm = true;
                            confirmId = sendId;
                        }
                        else if (Utility.Compare(sendId, confirmId) > 0)
                        {
                            confirmId = sendId;
                        }

                        break;
                    case CMD_PUSH: // 分片的序列号在接收窗口内，则进行确认并将分片添加到接收缓冲区，然后解析数据分片
                        if (Utility.Compare(sendId, nextReceiveId + receiveWindow) < 0)
                        {
                            messages.Add(new Message(sendId, sendTime));
                            if (Utility.Compare(sendId, nextReceiveId) >= 0)
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

                                Insert(segment);
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

            if (isConfirm)
            {
                Resend(confirmId);
            }

            if (Utility.Compare(serialId, previous) > 0) // 根据需要进行拥塞窗口的更新
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
                        increment += segmentSize * segmentSize / increment + segmentSize / 16;
                        if ((congestWindow + 1) * segmentSize <= increment)
                        {
                            congestWindow = (increment + segmentSize - 1) / (segmentSize > 0 ? segmentSize : 1);
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

        public void Update(long time)
        {
            sinceTime = (uint)time;
            if (!updated)
            {
                updated = true;
                refreshTime = sinceTime; //当前时间戳，表示最后一次刷新时间
            }

            var duration = Utility.Compare(sinceTime, refreshTime); //计算距离上次刷新的时间间隔

            if (Math.Abs(duration) > 10000)
            {
                duration = 0;
                refreshTime = sinceTime;
            }

            if (duration >= 0)
            {
                refreshTime += interval;
                if (sinceTime >= refreshTime)
                {
                    refreshTime = sinceTime + interval;
                }

                Refresh();
            }
        }

        public void Refresh()
        {
            if (!updated) return;
            var size = 0; // 要刷新的字节大小
            var lose = false; // 是否有丢失的片段
            var segment = pool.Pop();
            segment.id = current;
            segment.command = CMD_ACK;
            segment.window = receiveQueue.Count < receiveWindow ? receiveWindow - (uint)receiveQueue.Count : 0;
            segment.receiveId = nextReceiveId;
            
            foreach (var message in messages) // 更新确认
            {
                MakeSpace(HEAD);
                segment.sendId = message.sendId;
                segment.sendTime = message.sendTime;
                size += segment.Encode(buffer, size);
            }

            messages.Clear();
            if (remoteWindow == 0) // 探测窗口大小(如果远程窗口大小等于零)
            {
                if (probeWait == 0)
                {
                    probeWait = PROBE_DEF;
                    probeTime = sinceTime + probeWait;
                }
                else
                {
                    if (Utility.Compare(sinceTime, probeTime) >= 0)
                    {
                        if (probeWait < PROBE_DEF)
                        {
                            probeWait = PROBE_DEF;
                        }

                        probeWait += probeWait / 2;
                        if (probeWait > PROBE_MAX)
                        {
                            probeWait = PROBE_MAX;
                        }

                        probeTime = sinceTime + probeWait;
                        probe |= ASK_SEND;
                    }
                }
            }
            else
            {
                probeTime = 0;
                probeWait = 0;
            }

            if ((probe & ASK_SEND) != 0) // 刷新窗口探测命令
            {
                MakeSpace(HEAD);
                segment.command = CMD_WIN_ASK;
                size += segment.Encode(buffer, size);
            }

            if ((probe & ASK_TELL) != 0) // 刷新窗口探测命令
            {
                MakeSpace(HEAD);
                segment.command = CMD_WIN_INS;
                size += segment.Encode(buffer, size);
            }

            probe = 0;
            
            var windowSize = Math.Min(sendWindow, remoteWindow);
            
            while (Utility.Compare(nextSendId, serialId + windowSize) < 0)
            {
                if (sendQueue.Count == 0)
                {
                    break;
                }

                var send = sendQueue.Dequeue();
                send.id = current;
                send.command = CMD_PUSH;
                send.window = segment.window;
                send.sendTime = sinceTime;
                send.sendId = nextSendId;
                nextSendId += 1; // 增加下一段的序号
                send.receiveId = nextReceiveId;
                send.resendTime = sinceTime;
                send.rto = rto;
                send.resendId = 0;
                send.resendCount = 0;
                sends.Add(send);
            }

            int refresh = 0; // 刷新数据分段
            foreach (var send in sends)
            {
                var needSend = false;
                if (send.resendCount == 0) // 初始化传输
                {
                    needSend = true;
                    send.resendCount++;
                    send.rto = rto;
                    send.resendTime = sinceTime + (uint)send.rto;
                }
                else if (Utility.Compare(sinceTime, send.resendTime) >= 0) // 重传超时RTO
                {
                    needSend = true;
                    send.resendCount++;
                    send.rto += send.rto / 2;
                    send.resendTime = sinceTime + (uint)send.rto;
                    lose = true;
                }
                else if (send.resendId >= resend) // 快速重传确认
                {
                    if (send.resendCount <= resendLimit || resendLimit <= 0)
                    {
                        needSend = true;
                        send.resendCount++;
                        send.resendId = 0;
                        send.resendTime = sinceTime + (uint)send.rto;
                        refresh++;
                    }
                }

                if (needSend)
                {
                    send.sendTime = sinceTime;
                    send.window = segment.window;
                    send.receiveId = nextReceiveId;
                    MakeSpace(HEAD + (int)send.stream.Position);
                    size += send.Encode(buffer, size);

                    if (send.stream.Position > 0)
                    {
                        Buffer.BlockCopy(send.stream.GetBuffer(), 0, buffer, size, (int)send.stream.Position);
                        size += (int)send.stream.Position;
                    }

                    if (send.resendCount >= DEAD)
                    {
                        state = -1; // 如果消息被重发N次，则发生死链接
                    }
                }
            }

            pool.Push(segment);
            
            if (size > 0) // 刷新剩余的Buffer
            {
                onRefresh?.Invoke(buffer, size);
            }
            
            if (refresh > 0) //更新 慢启动阈值
            {
                var inflight = nextSendId - serialId;
                threshold = inflight / 2;
                if (threshold < THRESH_MIN)
                {
                    threshold = THRESH_MIN;
                }

                congestWindow = threshold + resend;
                increment = congestWindow * segmentSize;
            }

            if (lose)
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

            void MakeSpace(int space)
            {
                if (size + space > maxUnit)
                {
                    onRefresh?.Invoke(buffer, size);
                    size = 0;
                }
            }
        }
        
        /// <summary>
        /// 设置传输单位和分段大小
        /// </summary>
        /// <param name="maxUnit"></param>
        public void SetUnit(uint maxUnit)
        {
            this.maxUnit = Math.Max(maxUnit, 50);
            buffer = new byte[(maxUnit + HEAD) * 3];
            segmentSize = maxUnit - HEAD;
        }

        /// <summary>
        /// 设置重传和间隔
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="resend"></param>
        public void SetResend(uint interval, uint resend)
        {
            this.resend = resend;
            this.interval = Math.Clamp(interval, 10, 5000);
        }

        /// <summary>
        /// 设置窗口大小
        /// </summary>
        /// <param name="sendWindow"></param>
        /// <param name="receiveWindow"></param>
        public void SetWindow(uint sendWindow, uint receiveWindow)
        {
            this.sendWindow = sendWindow;
            this.receiveWindow = Math.Max(receiveWindow, WIN_REV);
        }

        /// <summary>
        /// 处理速度是否快速
        /// </summary>
        /// <returns></returns>
        public bool IsFaster()
        {
            if(receiveQueue.Count + sendQueue.Count + receives.Count + sends.Count > QUEUE_COUNT)
            {
                sends.Clear();
                return false;
            }

            return true;
        }
    }
}