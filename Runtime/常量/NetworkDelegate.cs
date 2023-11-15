namespace JFramework.Net
{
    /// <summary>
    /// 远程呼叫的委托
    /// </summary>
    public delegate void RpcDelegate(NetworkBehaviour behaviour, NetworkReader reader, UnityClient client);
    
    /// <summary>
    /// 网络消息的委托
    /// </summary>
    internal delegate void MessageDelegate(UnityPeer peer, NetworkReader reader, Channel channel);
}