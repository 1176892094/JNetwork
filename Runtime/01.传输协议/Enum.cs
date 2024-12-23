// *********************************************************************************
// # Project: JFramework
// # Unity: 6000.3.5f1
// # Author: 云谷千羽
// # Version: 1.0.0
// # History: 2024-11-29 13:11:20
// # Recently: 2024-12-22 20:12:12
// # Copyright: 2024, 云谷千羽
// # Description: This is an automatically generated comment.
// *********************************************************************************

namespace JFramework.Udp
{
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

    public enum Status : byte
    {
        Connect,
        Connected,
        Disconnect
    }

    public enum Reliable : byte
    {
        Connect = 1,
        Ping = 2,
        Data = 3,
    }

    public enum Unreliable : byte
    {
        Data = 4,
        Disconnect = 5,
    }
}