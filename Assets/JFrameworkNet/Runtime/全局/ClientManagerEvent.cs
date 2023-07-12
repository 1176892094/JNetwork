using System;
using JFramework.Interface;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JFramework.Net
{
    public static partial class ClientManager
    {
        /// <summary>
        /// 注册网络事件
        /// </summary>
        /// <param name="isHost">是否是基于主机的连接</param>
        private static void RegisterEvent(bool isHost)
        {
            if (isHost)
            {
                RegisterEvent<SpawnEvent>(OnSpawnByHost);
                RegisterEvent<DestroyEvent>(OnDestroyByHost);
                RegisterEvent<DespawnEvent>(OnEmptyEventByHost);
                RegisterEvent<PongEvent>(OnEmptyEventByHost);
            }
            else
            {
                RegisterEvent<SpawnEvent>(OnSpawnByClient);
                RegisterEvent<DestroyEvent>(OnDestroyByClient);
                RegisterEvent<DespawnEvent>(OnDespawnByClient);
                RegisterEvent<PongEvent>(OnPongByClient);
            }

            RegisterEvent<SnapshotEvent>(OnSnapshotEvent);
            RegisterEvent<ChangeOwnerEvent>(OnChangeOwnerEvent);
            RegisterEvent<RpcBufferEvent>(OnRpcBufferEvent);
        }

        /// <summary>
        /// 注册网络事件
        /// </summary>
        public static void RegisterEvent<T>(Action<T> handle, bool authority = true) where T : struct, IEvent
        {
            events[EventId<T>.Id] = NetworkEvent.Register(handle, authority);
        }

        /// <summary>
        /// 主机模式下空的网络事件
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        private static void OnEmptyEventByHost<T>(T message) where T : IEvent
        {
        }

        /// <summary>
        /// 主机模式下销毁游戏对象
        /// </summary>
        /// <param name="event"></param>
        private static void OnDestroyByHost(DestroyEvent @event)
        {
            if (spawns.TryGetValue(@event.netId, out var @object))
            {
                connection.observers.Remove(@object);
            }

            spawns.Remove(@event.netId);
        }

        /// <summary>
        /// 主机模式下生成物体的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnSpawnByHost(SpawnEvent @event)
        {
            if (ServerManager.spawns.TryGetValue(@event.netId, out var @object))
            {
                spawns[@event.netId] = @object;
                @object.isOwner = @event.isOwner;
                if (@event.isOwner)
                {
                    connection.observers.Add(@object);
                }

                @object.isClient = true;
                @object.OnNotifyAuthority();
                @object.OnStartClient();
            }
        }

        /// <summary>
        /// 客户端下隐藏物体的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnDespawnByClient(DespawnEvent @event)
        {
            if (spawns.TryGetValue(@event.netId, out var @object))
            {
                @object.OnStopClient();
                @object.gameObject.SetActive(false);
                @object.Reset();
                connection.observers.Remove(@object);
                spawns.Remove(@event.netId);
            }
        }

        /// <summary>
        /// 客户端下销毁物体的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnDestroyByClient(DestroyEvent @event)
        {
            if (spawns.TryGetValue(@event.netId, out var @object))
            {
                @object.OnStopClient();
                Object.Destroy(@object.gameObject);
                connection.observers.Remove(@object);
                spawns.Remove(@event.netId);
            }
        }

        /// <summary>
        /// 客户端下生成物体的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnSpawnByClient(SpawnEvent @event)
        {
            if (TrySpawn(@event, out var @object))
            {
                Spawn(@object, @event);
            }
        }

        /// <summary>
        /// 客户端从服务器接收的Ping
        /// </summary>
        /// <param name="event"></param>
        private static void OnPongByClient(PongEvent @event)
        {
            NetworkTime.OnClientPong();
        }

        /// <summary>
        /// 接收 远程过程调用(RPC) 缓存的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnRpcBufferEvent(RpcBufferEvent @event)
        {
        }

        /// <summary>
        /// 客户端下当游戏对象权限改变的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnChangeOwnerEvent(ChangeOwnerEvent @event)
        {
        }

        /// <summary>
        /// 客户端下网络消息快照的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnSnapshotEvent(SnapshotEvent @event)
        {
            NetworkSnapshot.OnTimeSnapshot(new TimeSnapshot(connection.timestamp, NetworkTime.localTime));
        }
    }
}