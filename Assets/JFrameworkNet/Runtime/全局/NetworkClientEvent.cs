using System;
using JFramework.Interface;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        /// <summary>
        /// 注册网络事件
        /// </summary>
        /// <param name="isHost">是否是基于主机的连接</param>
        private static void RegisterEvent(bool isHost)
        {
            if (isHost)
            {
                RegisterEvent<SpawnEvent>(SpawnByHost);
                RegisterEvent<ObjectDestroyEvent>(ObjectDestroyByHost);
                RegisterEvent<ObjectDespawnEvent>(OnEmptyMessageByHost);
                RegisterEvent<ObjectSpawnStartEvent>(OnEmptyMessageByHost);
                RegisterEvent<ObjectSpawnFinishEvent>(OnEmptyMessageByHost);
                RegisterEvent<PongEvent>(OnEmptyMessageByHost);
            }
            else
            {
                RegisterEvent<SpawnEvent>(SpawnByClient);
                RegisterEvent<ObjectDestroyEvent>(ObjectDestroyByClient);
                RegisterEvent<ObjectDespawnEvent>(ObjectDespawnByClient);
                RegisterEvent<ObjectSpawnStartEvent>(ObjectSpawnStartByClient);
                RegisterEvent<ObjectSpawnFinishEvent>(ObjectSpawnFinishByClient);
                RegisterEvent<PongEvent>(PongByClient);
            }

            RegisterEvent<SnapshotEvent>(OnSnapshotMessage);
            RegisterEvent<ChangeOwnerEvent>(OnOwnerChanged);
            RegisterEvent<RpcBufferEvent>(RpcBufferMessage);
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
        private static void OnEmptyMessageByHost<T>(T message) where T : IEvent
        {
        }

        /// <summary>
        /// 主机模式下销毁游戏对象
        /// </summary>
        /// <param name="event"></param>
        private static void ObjectDestroyByHost(ObjectDestroyEvent @event)
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
        private static void SpawnByHost(SpawnEvent @event)
        {
            if (NetworkServer.spawns.TryGetValue(@event.netId, out var @object))
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
        private static void ObjectDespawnByClient(ObjectDespawnEvent @event)
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
        private static void ObjectDestroyByClient(ObjectDestroyEvent @event)
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
        private static void SpawnByClient(SpawnEvent @event)
        {
        }

        /// <summary>
        /// 客户端从服务器接收的Ping
        /// </summary>
        /// <param name="event"></param>
        private static void PongByClient(PongEvent @event)
        {
            NetworkTime.OnClientPong();
        }

        /// <summary>
        /// 客户端下游戏对象开始生成的事件
        /// </summary>
        /// <param name="event"></param>
        private static void ObjectSpawnStartByClient(ObjectSpawnStartEvent @event)
        {
        }

        /// <summary>
        /// 客户端下游戏对象生成完成的事件
        /// </summary>
        /// <param name="event"></param>
        private static void ObjectSpawnFinishByClient(ObjectSpawnFinishEvent @event)
        {
        }

        /// <summary>
        /// 接收 远程过程调用(RPC) 缓存的事件
        /// </summary>
        /// <param name="event"></param>
        private static void RpcBufferMessage(RpcBufferEvent @event)
        {
        }

        /// <summary>
        /// 客户端下当游戏对象权限改变的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnOwnerChanged(ChangeOwnerEvent @event)
        {
        }

        /// <summary>
        /// 客户端下网络消息快照的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnSnapshotMessage(SnapshotEvent @event)
        {
            NetworkSnapshot.OnTimeSnapshot(new TimeSnapshot(connection.timestamp, NetworkTime.localTime));
        }
    }
}