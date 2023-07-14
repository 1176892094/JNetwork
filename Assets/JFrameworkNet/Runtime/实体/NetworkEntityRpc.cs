using System.Linq;
using UnityEngine;

namespace JFramework.Net
{
    public abstract partial class NetworkEntity
    {
        /// <summary>
        /// 服务器Rpc调用
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="methodHash">方法哈希值</param>
        /// <param name="writer">写入器</param>
        /// <param name="channel">传输通道</param>
        protected void SendServerRpcInternal(string methodName, int methodHash, NetworkWriter writer, Channel channel)
        {
            if (!ClientManager.isActive)
            {
                Debug.LogError($"调用 {methodName} 但是客户端不是活跃的。", gameObject);
                return;
            }

            if (!ClientManager.isReady)
            {
                Debug.LogWarning($"调用 {methodName} 但是客户端没有准备就绪的。对象名称{name}", gameObject);
                return;
            }

            if (!isOwner)
            {
                Debug.LogWarning($"调用 {methodName} 但是客户端没有对象权限。对象名称{name}", gameObject);
                return;
            }

            if (ClientManager.connection == null)
            {
                Debug.LogError($"调用 {methodName} 但是客户端的连接为空。对象名称{name}", gameObject);
                return;
            }

            ServerRpcEvent message = new ServerRpcEvent
            {
                netId = netId,
                component = component,
                methodHsh = (ushort)methodHash,
                segment = writer.ToArraySegment()
            };

            ClientManager.connection.Send(message, channel);
        }


        /// <summary>
        /// 客户端Rpc调用
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="methodHash">方法哈希值</param>
        /// <param name="writer">写入器</param>
        /// <param name="channel">传输通道</param>
        protected void SendClientRpcInternal(string methodName, int methodHash, NetworkWriter writer, Channel channel)
        {
            if (!ServerManager.isActive)
            {
                Debug.LogError($"调用 {methodName} 但是服务器不是活跃的。", gameObject);
                return;
            }

            if (!isServer)
            {
                Debug.LogWarning($"调用 {methodName} 但是对象没被创建。对象名称：{name}。", gameObject);
                return;
            }

            var @event = new ClientRpcEvent
            {
                netId = netId,
                component = component,
                methodHash = (ushort)methodHash,
                segment = writer.ToArraySegment()
            };

            using var writerObject = NetworkWriter.Pop();
            writerObject.Write(@event);

            foreach (var client in ServerManager.clients.Values.Where(client => client.isReady))
            {
                client.BufferRpc(@event, channel);
            }
        }

        /// <summary>
        /// 指定客户端Rpc调用
        /// </summary>
        /// <param name="client">传入指定客户端</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="methodHash">方法哈希值</param>
        /// <param name="writer">写入器</param>
        /// <param name="channel">传输通道</param>
        protected void SendTargetRpcInternal(ClientEntity client, string methodName, int methodHash, NetworkWriter writer, Channel channel)
        {
            if (!ServerManager.isActive)
            {
                Debug.LogError($"调用 {methodName} 但是服务器不是活跃的。", gameObject);
                return;
            }

            if (!isServer)
            {
                Debug.LogWarning($"调用 {methodName} 但是对象没被创建。对象名称：{name}。", gameObject);
                return;
            }

            client ??= connection;

            if (client is null)
            {
                Debug.LogError($"调用 {methodName} 但是对象的连接为空。对象名称：{name}", gameObject);
                return;
            }

            ClientRpcEvent @event = new ClientRpcEvent
            {
                netId = netId,
                component = component,
                methodHash = (ushort)methodHash,
                segment = writer.ToArraySegment()
            };

            client.BufferRpc(@event, channel);
        }
    }
}