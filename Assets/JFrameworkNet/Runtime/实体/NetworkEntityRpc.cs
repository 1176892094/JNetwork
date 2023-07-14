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
        protected void SendServerRpcInternal(string methodName, int methodHash, NetworkWriter writer, int channel)
        {
            if (!NetworkClient.isActive)
            {
                Debug.LogError($"调用 {methodName} 但是客户端不是活跃的。", gameObject);
                return;
            }

            if (!NetworkClient.isReady)
            {
                Debug.LogWarning($"调用 {methodName} 但是客户端没有准备就绪的。对象名称：{name}", gameObject);
                return;
            }

            if (!isOwner)
            {
                Debug.LogWarning($"调用 {methodName} 但是客户端没有对象权限。对象名称：{name}", gameObject);
                return;
            }

            if (NetworkClient.connection == null)
            {
                Debug.LogError($"调用 {methodName} 但是客户端的连接为空。对象名称：{name}", gameObject);
                return;
            }

            var @event = new ServerRpcEvent
            {
                objectId = objectId,
                serialId = serialId,
                methodHash = (ushort)methodHash,
                segment = writer.ToArraySegment()
            };

            NetworkClient.connection.Send(@event, (Channel)channel);
        }


        /// <summary>
        /// 客户端Rpc调用
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="methodHash">方法哈希值</param>
        /// <param name="writer">写入器</param>
        /// <param name="channel">传输通道</param>
        protected void SendClientRpcInternal(string methodName, int methodHash, NetworkWriter writer, int channel)
        {
            if (!NetworkServer.isActive)
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
                objectId = objectId,
                serialId = serialId,
                methodHash = (ushort)methodHash,
                segment = writer.ToArraySegment()
            };

            using var writerObject = NetworkWriter.Pop();
            writerObject.Write(@event);

            foreach (var client in NetworkServer.clients.Values.Where(client => client.isReady))
            {
                client.InvokeRpc(@event, (Channel)channel);
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
        protected void SendTargetRpcInternal(NetworkClientEntity client, string methodName, int methodHash, NetworkWriter writer, int channel)
        {
            if (!NetworkServer.isActive)
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

            var @event = new ClientRpcEvent
            {
                objectId = objectId,
                serialId = serialId,
                methodHash = (ushort)methodHash,
                segment = writer.ToArraySegment()
            };

            client.InvokeRpc(@event, (Channel)channel);
        }
    }
}