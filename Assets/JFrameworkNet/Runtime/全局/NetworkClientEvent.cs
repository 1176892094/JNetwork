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
            Debug.Log("注册客户端事件");
            if (isHost)
            {
                RegisterEvent<SpawnEvent>(OnSpawnByHost);
                RegisterEvent<DestroyEvent>(OnDestroyByHost);
                RegisterEvent<DespawnEvent>(OnEmptyEventByHost);
                RegisterEvent<PongEvent>(OnEmptyEventByHost);
                RegisterEvent<EntityEvent>(OnEmptyEventByHost);
            }
            else
            {
                RegisterEvent<SpawnEvent>(OnSpawnByClient);
                RegisterEvent<DestroyEvent>(OnDestroyByClient);
                RegisterEvent<DespawnEvent>(OnDespawnByClient);
                RegisterEvent<PongEvent>(NetworkTime.OnPongEvent);
                RegisterEvent<EntityEvent>(OnEntityEvent);
            }

            RegisterEvent<NotReadyEvent>(OnNotReadyEvent);
            RegisterEvent<SceneEvent>(OnSceneEvent);
            RegisterEvent<TimeEvent>(OnTimeEvent);
            RegisterEvent<InvokeRpcEvent>(OnInvokeRpcEvent);
        }

        /// <summary>
        /// 注册网络事件
        /// </summary>
        private static void RegisterEvent<T>(Action<T> handle) where T : struct, IEvent
        {
            events[NetworkEvent<T>.Id] = NetworkEvent.Register(handle);
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
            spawns.Remove(@event.objectId);
        }

        /// <summary>
        /// 主机模式下生成物体的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnSpawnByHost(SpawnEvent @event)
        {
            if (NetworkServer.spawns.TryGetValue(@event.objectId, out var @object))
            {
                spawns[@event.objectId] = @object;
                @object.isOwner = @event.isOwner;
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
            if (spawns.TryGetValue(@event.objectId, out var @object))
            {
                @object.OnStopClient();
                @object.gameObject.SetActive(false);
                @object.Reset();
                spawns.Remove(@event.objectId);
            }
        }

        /// <summary>
        /// 客户端下销毁物体的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnDestroyByClient(DestroyEvent @event)
        {
            if (spawns.TryGetValue(@event.objectId, out var @object))
            {
                @object.OnStopClient();
                Object.Destroy(@object.gameObject);
                spawns.Remove(@event.objectId);
            }
        }

        /// <summary>
        /// 客户端下生成物体的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnSpawnByClient(SpawnEvent @event)
        {
            isSpawn = false;
            SpawnStart();
            if (TrySpawn(@event, out var @object))
            {
                Spawn(@object, @event);
            }
            
            SpawnFinish();
            isSpawn = true;
        }
        
        /// <summary>
        /// 接收 远程过程调用(RPC) 缓存的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnInvokeRpcEvent(InvokeRpcEvent @event)
        {
            using var reader = NetworkReader.Pop(@event.segment);
            while (reader.Residue > 0)
            {
                var clientRpc = reader.Read<ClientRpcEvent>();
                OnClientRpcEvent(clientRpc);
            }
        }
        
        /// <summary>
        /// 当接收到 ClientRpc 的消息
        /// </summary>
        /// <param name="event"></param>
        private static void OnClientRpcEvent(ClientRpcEvent @event)
        {
            if (!spawns.TryGetValue(@event.objectId, out var @object)) return;
            using var reader = NetworkReader.Pop(@event.segment);
            @object.InvokeRpcEvent(@event.serialId, @event.methodHash, RpcType.ClientRpc, reader);
        }

        /// <summary>
        /// 客户端下网络消息快照的事件
        /// </summary>
        /// <param name="event"></param>
        private static void OnTimeEvent(TimeEvent @event)
        {
            NetworkSnapshot.OnTimeSnapshot(new TimeSnapshot(connection.timestamp, NetworkTime.localTime));
        }

        /// <summary>
        /// 实体状态同步
        /// </summary>
        /// <param name="event"></param>
        private static void OnEntityEvent(EntityEvent @event)
        {
            if (spawns.TryGetValue(@event.objectId, out var @object) && @object != null)
            {
                using var reader = NetworkReader.Pop(@event.segment);
                @object.DeserializeClient(reader, false);
            }
            else
            {
                Debug.LogWarning($"没有为 {@event.objectId} 的同步消息找到目。");
            }
        }

        /// <summary>
        /// 客户端场景改变
        /// </summary>
        /// <param name="event"></param>
        private static void OnSceneEvent(SceneEvent @event)
        {
            if (isAuthority)
            {
                NetworkManager.ClientLoadScene(@event.sceneName);
            }
        }
        
        /// <summary>
        /// 客户端未准备就绪的事件 (不能接收和发送消息)
        /// </summary>
        /// <param name="event"></param>
        private static void OnNotReadyEvent(NotReadyEvent @event)
        {
            isReady = false;
            OnClientNotReady?.Invoke();
        }
    }
}