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
            RegisterEvent<ReadyEvent>(OnServerReadyEvent);
            RegisterEvent<ServerRpcEvent>(OnServerRpcEvent);
            RegisterEvent<PingEvent>(NetworkTime.OnPingEvent);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterEvent<T>(Action<NetworkClientEntity, T> handle) where T : struct, IEvent
        {
            events[NetworkEvent<T>.Id] = NetworkEvent.Register(handle);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterEvent<T>(Action<NetworkClientEntity, T, Channel> handle) where T : struct, IEvent
        {
            events[NetworkEvent<T>.Id] = NetworkEvent.Register(handle);
        }

        /// <summary>
        /// 当发送一条命令到Transport
        /// </summary>
        private static void OnServerRpcEvent(NetworkClientEntity client, ServerRpcEvent @event, Channel channel)
        {
            if (!client.isReady)
            {
                Debug.LogWarning("接收到 ServerRpc 但客户端没有准备就绪");
            }
            else if (!spawns.TryGetValue(@event.netId, out var @object))
            {
                Debug.LogWarning($"没有找到发送 ServerRpc 的对象。对象网络Id：{@event.netId}");
            }
            else if (RpcUtils.GetAuthorityByHash(@event.methodHsh) && @object.connection != client)
            {
                Debug.LogWarning($"接收到 ServerRpc 但对象没有通过验证。对象网络Id：{@event.netId}");
            }
            else
            {
                using var reader = NetworkReader.Pop(@event.segment);
                @object.InvokeRpcEvent(@event.component, @event.methodHsh, RpcType.ServerRpc, reader, client);
            }
        }

        /// <summary>
        /// 当客户端在服务器准备就绪，向客户端发送生成物体的消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="event"></param>
        private static void OnServerReadyEvent(NetworkClientEntity client, ReadyEvent @event)
        {
            SetClientReady(client);
            OnServerReady?.Invoke(client);
        }
    }
}