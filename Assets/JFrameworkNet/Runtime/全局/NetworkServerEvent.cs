using System;
using JFramework.Interface;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        /// <summary>
        /// 注册服务器消息事件
        /// </summary>
        private static void RegisterEvent()
        {
            Debug.Log("注册服务器事件");
            RegisterEvent<EntityEvent>(OnEntityEvent);
            RegisterEvent<SetReadyEvent>(OnSetReadyEvent);
            RegisterEvent<ServerRpcEvent>(OnServerRpcEvent);
            RegisterEvent<PingEvent>(NetworkTime.OnPingEvent);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterEvent<T>(Action<ClientEntity, T> handle) where T : struct, IEvent
        {
            events[NetworkEvent<T>.Id] = NetworkEvent.Register(handle);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterEvent<T>(Action<ClientEntity, T, Channel> handle) where T : struct, IEvent
        {
            events[NetworkEvent<T>.Id] = NetworkEvent.Register(handle);
        }

        /// <summary>
        /// 当发送一条Rpc到Transport
        /// </summary>
        private static void OnServerRpcEvent(ClientEntity client, ServerRpcEvent @event, Channel channel)
        {
            if (!client.isReady)
            {
                Debug.LogWarning("接收到 ServerRpc 但客户端没有准备就绪");
            }
            else if (!spawns.TryGetValue(@event.objectId, out var @object))
            {
                Debug.LogWarning($"没有找到发送 ServerRpc 的对象。对象网络Id：{@event.objectId}");
            }
            else if (NetworkRpc.HasAuthority(@event.methodHash) && @object.client != client)
            {
                Debug.LogWarning($"接收到 ServerRpc 但对象没有通过验证。对象网络Id：{@event.objectId}");
            }
            else
            {
                using var reader = NetworkReader.Pop(@event.segment);
                @object.InvokeRpcEvent(@event.serialId, @event.methodHash, RpcType.ServerRpc, reader, client);
            }
        }
        
        /// <summary>
        /// 实体状态同步事件
        /// </summary>
        /// <param name="client"></param>
        /// <param name="event"></param>
        private static void OnEntityEvent(ClientEntity client, EntityEvent @event)
        {
            if (spawns.TryGetValue(@event.objectId, out var @object) && @object != null)
            {
                if (@object.client == client)
                {
                    using var reader = NetworkReader.Pop(@event.segment);
                    if (!@object.ServerDespawn(reader))
                    {
                        Debug.LogWarning($"无法反序列化对象：{@object.name}。对象Id：{@object.objectId}");
                        client.Disconnect();
                    }
                }
                else
                {
                    Debug.LogWarning($"网络对象 {client} 为 {@object} 发送的 ObjectEvent 没有权限");
                }
            }
        }


        /// <summary>
        /// 当客户端在服务器准备就绪，向客户端发送生成物体的消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="event"></param>
        private static void OnSetReadyEvent(ClientEntity client, SetReadyEvent @event)
        {
            SetReadyForClient(client);
            OnServerReady?.Invoke(client);
        }
    }
}