using System;
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
            RegisterMessage<EntityMessage>(OnEntityByServer);
            RegisterMessage<SetReadyMessage>(OnSetReadyByServer);
            RegisterMessage<ServerRpcMessage>(OnServerRpcByServer);
            RegisterMessage<PingMessage>(NetworkTime.OnPingByServer);
            RegisterMessage<SnapshotMessage>(OnSnapshotByServer);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterMessage<TMessage>(Action<UnityClient, TMessage> handle) where TMessage : struct, Message
        {
            messages[NetworkMessage<TMessage>.Id] = NetworkMessage.Register(handle);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterMessage<TMessage>(Action<UnityClient, TMessage, Channel> handle) where TMessage : struct, Message
        {
            messages[NetworkMessage<TMessage>.Id] = NetworkMessage.Register(handle);
        }

        /// <summary>
        /// 当从Transport接收到一条ServerRpc消息
        /// </summary>
        private static void OnServerRpcByServer(UnityClient client, ServerRpcMessage message, Channel channel)
        {
            if (!client.isReady)
            {
                if (channel == Channel.Reliable)
                {
                    Debug.LogWarning("接收到 ServerRpc 但客户端没有准备就绪");
                }

                return;
            }

            if (!spawns.TryGetValue(message.objectId, out var @object))
            {
                Debug.LogWarning($"没有找到发送 ServerRpc 的对象。对象网络Id：{message.objectId}");
                return;
            }

            if (NetworkRpc.HasAuthority(message.methodHash) && @object.connection != client)
            {
                Debug.LogWarning($"接收到 ServerRpc 但对象没有通过验证。对象网络Id：{message.objectId}");
                return;
            }

            using var reader = NetworkReader.Pop(message.segment);
            @object.InvokeRpcMessage(message.serialId, message.methodHash, RpcType.ServerRpc, reader, client);
        }

        /// <summary>
        /// 当接收一条快照消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private static void OnSnapshotByServer(UnityClient client, SnapshotMessage message)
        {
            client?.OnSnapshotMessage(new SnapshotTime(client.remoteTime, NetworkTime.localTime));
        }

        /// <summary>
        /// 实体状态同步消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private static void OnEntityByServer(UnityClient client, EntityMessage message)
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
        private static void OnSetReadyByServer(UnityClient client, SetReadyMessage message)
        {
            SetReadyForClient(client);
            NetworkManager.Instance.SpawnPrefab(client);
            OnServerReady?.Invoke(client);
        }
    }
}