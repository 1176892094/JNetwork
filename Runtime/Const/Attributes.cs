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
        private int channel;
        public ClientRpcAttribute() => channel = Channel.Reliable;
        public ClientRpcAttribute(int channel) => this.channel = channel;
    }

    /// <summary>
    /// 由客户端向服务器调用方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerRpcAttribute : Attribute
    {
        private int channel;
        public ServerRpcAttribute() => channel = Channel.Reliable;
        public ServerRpcAttribute(int channel) => this.channel = channel;
    }

    /// <summary>
    /// 服务器向指定客户端调用方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TargetRpcAttribute : Attribute
    {
        private int channel;
        public TargetRpcAttribute() => channel = Channel.Reliable;
        public TargetRpcAttribute(int channel) => this.channel = channel;
    }

    /// <summary>
    /// 服务器变量，当变量改变后，向所有客户端同步
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SyncVarAttribute : Attribute
    {
        private string func;
        public SyncVarAttribute() => func = null;
        public SyncVarAttribute(string func) => this.func = func;
    }
}