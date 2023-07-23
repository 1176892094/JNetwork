using System;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        /// <summary>
        /// 注册服务器消息消息
        /// </summary>
        private static void RegisterMessage()
        {
            Debug.Log("注册服务器网络消息");
            RegisterMessage<EntityMessage>(OnEntityByServer);
            RegisterMessage<SetReadyMessage>(OnSetReadyByServer);
            RegisterMessage<ServerRpcMessage>(OnServerRpcByServer);
            RegisterMessage<PingMessage>(NetworkTime.OnPingByServer);
            RegisterMessage<SnapshotMessage>(OnSnapshotByServer);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterMessage<T>(Action<ClientEntity, T> handle) where T : struct, IEvent
        {
            messages[NetworkMessage<T>.Id] = NetworkMessage.Register(handle);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterMessage<T>(Action<ClientEntity, T, Channel> handle) where T : struct, IEvent
        {
            messages[NetworkMessage<T>.Id] = NetworkMessage.Register(handle);
        }

        /// <summary>
        /// 当从Transport接收到一条ServerRpc消息
        /// </summary>
        private static void OnServerRpcByServer(ClientEntity client, ServerRpcMessage message, Channel channel)
        {
            if (!client.isReady)
            {
                Debug.LogWarning("接收到 ServerRpc 但客户端没有准备就绪");
            }
            else if (!spawns.TryGetValue(message.objectId, out var @object))
            {
                Debug.LogWarning($"没有找到发送 ServerRpc 的对象。对象网络Id：{message.objectId}");
            }
            else if (NetworkRpc.HasAuthority(message.methodHash) && @object.connection != client)
            {
                Debug.LogWarning($"接收到 ServerRpc 但对象没有通过验证。对象网络Id：{message.objectId}");
            }
            else
            {
                using var reader = NetworkReader.Pop(message.segment);
                @object.InvokeRpcMessage(message.serialId, message.methodHash, RpcType.ServerRpc, reader, client);
            }
        }
        
        /// <summary>
        /// 当接收一条快照消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="snapshot"></param>
        private static void OnSnapshotByServer(ClientEntity client, SnapshotMessage snapshot)
        {
            
        }
        
        /// <summary>
        /// 实体状态同步消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private static void OnEntityByServer(ClientEntity client, EntityMessage message)
        {
            if (spawns.TryGetValue(message.objectId, out var @object) && @object != null)
            {
                if (@object.connection == client)
                {
                    using var reader = NetworkReader.Pop(message.segment);
                    if (!@object.ServerDeserialize(reader))
                    {
                        Debug.LogWarning($"无法反序列化对象：{@object.name}。对象Id：{@object.objectId}");
                        client.Disconnect();
                    }
                }
                else
                {
                    Debug.LogWarning($"网络对象 {client} 为 {@object} 发送的 EntityMessage 没有权限");
                }
            }
        }


        /// <summary>
        /// 当客户端在服务器准备就绪，向客户端发送生成物体的消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private static void OnSetReadyByServer(ClientEntity client, SetReadyMessage message)
        {
            SetReadyForClient(client);
            OnServerReady?.Invoke(client);
        }
    }
}