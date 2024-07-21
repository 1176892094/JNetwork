// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  02:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;

namespace JFramework.Net
{
    /// <summary>
    /// 由服务器向客户端调用方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpcAttribute : Attribute
    {
        private byte channel;
        private SendMode mode;

        public ClientRpcAttribute(byte channel = Channel.Reliable, SendMode mode = SendMode.Total)
        {
            this.mode = mode;
            this.channel = channel;
        }
    }

    /// <summary>
    /// 由客户端向服务器调用方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerRpcAttribute : Attribute
    {
        private byte channel;
        public ServerRpcAttribute(byte channel = Channel.Reliable) => this.channel = channel;
    }

    /// <summary>
    /// 服务器向指定客户端调用方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TargetRpcAttribute : Attribute
    {
        private byte channel;
        public TargetRpcAttribute(byte channel = Channel.Reliable) => this.channel = channel;
    }

    /// <summary>
    /// 服务器变量，当变量改变后，向所有客户端同步
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SyncVarAttribute : Attribute
    {
        private string func;
        public SyncVarAttribute(string func = null) => this.func = func;
    }
}