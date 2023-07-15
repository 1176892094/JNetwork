using System;
using UnityEngine;
// ReSharper disable All

namespace JFramework.Net
{
    /// <summary>
    /// 由服务器向客户端调用方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpcAttribute : Attribute
    {
        private Channel channel;
        public ClientRpcAttribute() => channel = Channel.Reliable;
        public ClientRpcAttribute(Channel channel = Channel.Reliable) => this.channel = channel;
    }

    /// <summary>
    /// 由客户端向服务器调用方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerRpcAttribute : Attribute
    {
        private Channel channel;
        public ServerRpcAttribute() => channel = Channel.Reliable;
        public ServerRpcAttribute(Channel channel = Channel.Reliable) => this.channel = channel;
    }

    /// <summary>
    /// 服务器向指定客户端调用方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TargetRpcAttribute : Attribute
    {
        private Channel channel;
        public TargetRpcAttribute() => channel = Channel.Reliable;
        public TargetRpcAttribute(Channel channel = Channel.Reliable) => this.channel = channel;
    }

    /// <summary>
    /// 服务器变量，当变量改变后，向所有客户端同步
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SyncVarAttribute : PropertyAttribute
    {
        private string onValueChanged;
        public SyncVarAttribute() => onValueChanged = null;
        public SyncVarAttribute(string onValueChanged = null) => this.onValueChanged = onValueChanged;
    }
}