// *********************************************************************************
// # Project: Forest
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-08-27  15:08
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using JFramework.Interface;

namespace JFramework.Net
{
    /// <summary>
    /// 当开启服务器
    /// </summary>
    public struct OnStartServer : IEvent
    {
    }

    /// <summary>
    /// 当开启客户端
    /// </summary>
    public struct OnStartClient : IEvent
    {
    }

    /// <summary>
    /// 当停止服务器
    /// </summary>
    public struct OnStopServer : IEvent
    {
    }

    /// <summary>
    /// 当停止客户端
    /// </summary>
    public struct OnStopClient : IEvent
    {
    }

    /// <summary>
    /// 客户端 发送到 服务器 再返回到 客户端 时间
    /// </summary>
    public struct OnPingUpdate : IEvent
    {
        public readonly double roundTripTime;

        public OnPingUpdate(double roundTripTime) => this.roundTripTime = roundTripTime;
    }
    
    /// <summary>
    /// 客户端加载场景的事件
    /// </summary>
    public struct OnClientChangeScene : IEvent
    {
        public readonly string sceneName;

        public OnClientChangeScene(string sceneName) => this.sceneName = sceneName;
    }

    /// <summary>
    /// 服务器加载场景的事件
    /// </summary>
    public struct OnServerChangeScene : IEvent
    {
        public readonly string sceneName;

        public OnServerChangeScene(string sceneName) => this.sceneName = sceneName;
    }

    /// <summary>
    /// 客户端加载场景完成的事件
    /// </summary>
    public struct OnClientSceneChanged : IEvent
    {
        public readonly string sceneName;

        public OnClientSceneChanged(string sceneName) => this.sceneName = sceneName;
    }

    /// <summary>
    /// 服务器加载场景完成的事件
    /// </summary>
    public struct OnServerSceneChanged : IEvent
    {
        public readonly string sceneName;

        public OnServerSceneChanged(string sceneName) => this.sceneName = sceneName;
    }

    /// <summary>
    /// 当客户端连接到服务器
    /// </summary>
    public struct OnServerConnect : IEvent
    {
        public readonly NetworkClient client;

        public OnServerConnect(NetworkClient client) => this.client = client;
    }

    /// <summary>
    /// 当客户端从服务器断开
    /// </summary>
    public struct OnServerDisconnect : IEvent
    {
        public readonly NetworkClient client;

        public OnServerDisconnect(NetworkClient client) => this.client = client;
    }

    /// <summary>
    /// 当客户端准备就绪 (场景加载完成)
    /// </summary>
    public struct OnServerReady : IEvent
    {
        public readonly NetworkClient client;

        public OnServerReady(NetworkClient client) => this.client = client;
    }

    /// <summary>
    /// 当客户端连接
    /// </summary>
    public struct OnClientConnect : IEvent
    {
    }

    /// <summary>
    /// 当客户端断开
    /// </summary>
    public struct OnClientDisconnect : IEvent
    {
    }

    /// <summary>
    /// 当客户端取消准备 (加载场景)
    /// </summary>
    public struct OnClientNotReady : IEvent
    {
    }
}