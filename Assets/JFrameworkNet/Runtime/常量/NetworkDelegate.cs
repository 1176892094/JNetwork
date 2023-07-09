using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 客户端权限改变
    /// </summary>
    public delegate void AuthorityDelegate(ClientEntity client, NetworkObject @object, bool authority);
    
    /// <summary>
    /// 远程呼叫的委托
    /// </summary>
    public delegate void RpcDelegate(NetworkEntity entity, NetworkReader reader, ClientEntity client);
    
    /// <summary>
    /// 网络消息的委托
    /// </summary>
    public delegate void EventDelegate(Connection connection, NetworkReader reader, Channel channel);

    /// <summary>
    /// 生成处理的委托
    /// </summary>
    public delegate GameObject SpawnDelegate(SpawnEvent @event);
    
    /// <summary>
    /// 销毁处理的委托
    /// </summary>
    public delegate void DespawnDelegate(GameObject obj);
}