// *********************************************************************************
// # Project: Forest
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-12-03  13:12
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System.Linq;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 远程调用模块
    /// </summary>
    public abstract partial class NetworkBehaviour
    {
        /// <summary>
        /// TODO:自动生成代码调用服务器Rpc
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="methodHash">方法哈希值</param>
        /// <param name="writer">写入器</param>
        /// <param name="channel">传输通道</param>
        protected void SendServerRpcInternal(string methodName, int methodHash, NetworkWriter writer, int channel)
        {
            if (!NetworkManager.Client.isActive)
            {
                Debug.LogError($"调用 {methodName} 但是客户端不是活跃的。", gameObject);
                return;
            }

            if (!NetworkManager.Client.isReady)
            {
                Debug.LogWarning($"调用 {methodName} 但是客户端没有准备就绪的。对象名称：{name}", gameObject);
                return;
            }

            var requireReady = (channel & Channel.NotReady) != Channel.NotReady;
            if (!(!requireReady || isOwner))
            {
                Debug.LogWarning($"调用 {methodName} 但是客户端没有对象权限。对象名称：{name}", gameObject);
                return;
            }

            if (NetworkManager.Client.connection == null)
            {
                Debug.LogError($"调用 {methodName} 但是客户端的连接为空。对象名称：{name}", gameObject);
                return;
            }

            var message = new ServerRpcMessage
            {
                objectId = objectId,
                componentId = componentId,
                methodHash = (ushort)methodHash,
                segment = writer,
            };

            channel = (channel & Channel.Reliable) == Channel.Reliable ? Channel.Reliable : Channel.Unreliable;
            NetworkManager.Client.connection.Send(message, channel);
        }


        /// <summary>
        /// TODO:自动生成代码调用客户端Rpc
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="methodHash">方法哈希值</param>
        /// <param name="writer">写入器</param>
        /// <param name="channel">传输通道</param>
        protected void SendClientRpcInternal(string methodName, int methodHash, NetworkWriter writer, int channel)
        {
            if (!NetworkManager.Server.isActive)
            {
                Debug.LogError($"调用 {methodName} 但是服务器不是活跃的。", gameObject);
                return;
            }

            if (!isServer)
            {
                Debug.LogWarning($"调用 {methodName} 但是对象没被创建。对象名称：{name}。", gameObject);
                return;
            }

            var message = new ClientRpcMessage
            {
                objectId = objectId,
                componentId = componentId,
                methodHash = (ushort)methodHash,
                segment = writer
            };

            using var current = NetworkWriter.Pop();
            current.Invoke(message);

            var includeOwner = (channel & Channel.NotOwner) != Channel.NotOwner;
            channel = (channel & Channel.Reliable) == Channel.Reliable ? Channel.Reliable : Channel.Unreliable;
            foreach (var client in NetworkManager.Server.clients.Values.Where(client => client.isReady))
            {
                if (includeOwner || client != connection)
                {
                    client.Send(message, channel);
                }
            }
        }

        /// <summary>
        /// TODO:自动生成代码调用指定客户端Rpc
        /// </summary>
        /// <param name="client">传入指定客户端</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="methodHash">方法哈希值</param>
        /// <param name="writer">写入器</param>
        /// <param name="channel">传输通道</param>
        protected void SendTargetRpcInternal(NetworkClient client, string methodName, int methodHash, NetworkWriter writer, int channel)
        {
            if (!NetworkManager.Server.isActive)
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

            if (client == null)
            {
                Debug.LogError($"调用 {methodName} 但是对象的连接为空。对象名称：{name}", gameObject);
                return;
            }

            var message = new ClientRpcMessage
            {
                objectId = objectId,
                componentId = componentId,
                methodHash = (ushort)methodHash,
                segment = writer
            };
          
            client.Send(message, channel);
        }
    }
}