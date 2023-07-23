namespace JFramework.Interface
{
    public interface INetworkEvent
    {
    }

    public interface IStartClient : INetworkEvent
    {
        void OnStartClient();
    }

    public interface IStopClient : INetworkEvent
    {
        void OnStopClient();
    }

    public interface IStartServer : INetworkEvent
    {
        void OnStartServer();
    }

    public interface IStopServer : INetworkEvent
    {
        void OnStopServer();
    }

    public interface IStartAuthority : INetworkEvent
    {
        void OnStartAuthority();
    }

    public interface IStopAuthority : INetworkEvent
    {
        void OnStopAuthority();
    }
}