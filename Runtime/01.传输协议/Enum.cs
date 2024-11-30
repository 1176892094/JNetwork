namespace JFramework.Udp
{
    public enum State : byte
    {
        Connect,
        Connected,
        Disconnect
    }

    public enum Error : byte
    {
        DnsResolve = 1,       // 无法解析主机地址
        Timeout = 2,          // Ping链接超时
        Congestion = 3,       // 传输网络无法处理更多的消息
        InvalidReceive = 4,   // 接收到无效数据包（可能是故意攻击）
        InvalidSend = 5,      // 用户试图发送无效数据
        ConnectionClosed = 6, // 连接自动关闭或非自愿丢失
        Unexpected = 7        // 意外错误异常，需要修复
    }

    public enum ReliableHeader : byte
    {
        Connect = 1,
        Ping = 2,
        Data = 3,
    }

    public enum UnreliableHeader : byte
    {
        Data = 4,
        Disconnect = 5,
    }
}