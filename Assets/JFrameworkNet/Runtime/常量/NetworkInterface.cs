namespace JFramework.Interface
{
    /// <summary>
    /// 网络事件接口
    /// </summary>
    public interface NetworkEvent
    {
    }

    /// <summary>
    /// 在客户端对象 初始化时调用 (服务器不调用)
    /// </summary>
    public interface IStartClient : NetworkEvent
    {
        void OnStartClient();
    }

    /// <summary>
    /// 在客户端对象 停止或断开时调用 (服务器不调用)
    /// </summary>
    public interface IStopClient : NetworkEvent
    {
        void OnStopClient();
    }

    /// <summary>
    /// 当客户端对象 从服务器初始化时调用 (客户端不调用)
    /// </summary>
    public interface IStartServer : NetworkEvent
    {
        void OnStartServer();
    }

    /// <summary>
    /// 当客户端对象 从服务器停止或断开时调用 (客户端不调用)
    /// </summary>
    public interface IStopServer : NetworkEvent
    {
        void OnStopServer();
    }

    /// <summary>
    /// 当玩家获得 客户端对象的权限 时调用 (服务器不调用)
    /// </summary>
    public interface IStartAuthority : NetworkEvent
    {
        void OnStartAuthority();
    }

    /// <summary>
    /// 当玩家丢失 客户端对象的权限 时调用 (服务器不调用)
    /// </summary>
    public interface IStopAuthority : NetworkEvent
    {
        void OnStopAuthority();
    }
}