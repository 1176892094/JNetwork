using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        /// <summary>
        /// 注册网络消息
        /// </summary>
        /// <param name="isHost">是否是基于主机的连接</param>
        private static void RegisterMessage(bool isHost)
        {
            Debug.Log("注册客户端网络消息");
            if (isHost)
            {
                RegisterMessage<SpawnMessage>(OnSpawnByHost);
                RegisterMessage<DestroyMessage>(OnEmptyByHost);
                RegisterMessage<DespawnMessage>(OnEmptyByHost);
                RegisterMessage<PongMessage>(OnEmptyByHost);
                RegisterMessage<EntityMessage>(OnEmptyByHost);
            }
            else
            {
                RegisterMessage<SpawnMessage>(OnSpawnByClient);
                RegisterMessage<DestroyMessage>(OnDestroyByClient);
                RegisterMessage<DespawnMessage>(OnDespawnByClient);
                RegisterMessage<PongMessage>(NetworkTime.OnPongEvent);
                RegisterMessage<EntityMessage>(OnEntityEvent);
            }

            RegisterMessage<NotReadyMessage>(OnNotReadyByClient);
            RegisterMessage<SceneMessage>(OnSceneByClient);
            RegisterMessage<TimeMessage>(OnTimeByClient);
            RegisterMessage<InvokeRpcMessage>(OnInvokeRpcByClient);
        }

        /// <summary>
        /// 注册网络消息
        /// </summary>
        private static void RegisterMessage<T>(Action<T> handle) where T : struct, IMessage
        {
            messages[NetworkMessage<T>.Id] = NetworkMessage.Register(handle);
        }

        /// <summary>
        /// 主机模式下空的网络消息
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        private static void OnEmptyByHost<T>(T message) where T : IMessage
        {
        }
        
        /// <summary>
        /// 主机模式下生成物体的消息
        /// </summary>
        /// <param name="message"></param>
        private static void OnSpawnByHost(SpawnMessage message)
        {
            if (NetworkServer.spawns.TryGetValue(message.objectId, out var @object))
            {
                spawns[message.objectId] = @object;
                @object.isOwner = message.isOwner;
                @object.isClient = true;
                @object.OnNotifyAuthority();
                @object.OnStartClient();
            }
        }

        /// <summary>
        /// 客户端下隐藏物体的消息
        /// </summary>
        /// <param name="message"></param>
        private static void OnDespawnByClient(DespawnMessage message)
        {
            if (spawns.TryGetValue(message.objectId, out var @object))
            {
                @object.OnStopClient();
                @object.gameObject.SetActive(false);
                @object.Reset();
                spawns.Remove(message.objectId);
            }
        }

        /// <summary>
        /// 客户端下销毁物体的消息
        /// </summary>
        /// <param name="message"></param>
        private static void OnDestroyByClient(DestroyMessage message)
        {
            if (spawns.TryGetValue(message.objectId, out var @object))
            {
                @object.OnStopClient();
                Object.Destroy(@object.gameObject);
                spawns.Remove(message.objectId);
            }
        }

        /// <summary>
        /// 客户端下生成物体的消息
        /// </summary>
        /// <param name="message"></param>
        private static void OnSpawnByClient(SpawnMessage message)
        {
            isSpawn = false;
            SpawnStart();
            if (TrySpawn(message, out var @object))
            {
                Spawn(@object, message);
            }
            
            SpawnFinish();
            isSpawn = true;
        }
        
        /// <summary>
        /// 接收 远程过程调用(RPC) 缓存的消息
        /// </summary>
        /// <param name="message"></param>
        private static void OnInvokeRpcByClient(InvokeRpcMessage message)
        {
            using var reader = NetworkReader.Pop(message.segment);
            while (reader.Residue > 0)
            {
                var clientRpc = reader.Read<ClientRpcMessage>();
                OnClientRpcEvent(clientRpc);
            }
        }
        
        /// <summary>
        /// 当接收到 ClientRpc 的消息
        /// </summary>
        /// <param name="message"></param>
        private static void OnClientRpcEvent(ClientRpcMessage message)
        {
            if (!spawns.TryGetValue(message.objectId, out var @object)) return;
            using var reader = NetworkReader.Pop(message.segment);
            @object.InvokeRpcMessage(message.serialId, message.methodHash, RpcType.ClientRpc, reader);
        }

        /// <summary>
        /// 客户端下网络消息快照的消息
        /// </summary>
        /// <param name="message"></param>
        private static void OnTimeByClient(TimeMessage message)
        {
          //  NetworkSnapshot.OnTimeSnapshot(new TimeSnapshot(server.timestamp, NetworkTime.localTime));
        }

        /// <summary>
        /// 实体状态同步
        /// </summary>
        /// <param name="message"></param>
        private static void OnEntityEvent(EntityMessage message)
        {
            if (spawns.TryGetValue(message.objectId, out var @object) && @object != null)
            {
                using var reader = NetworkReader.Pop(message.segment);
                @object.ClientDeserialize(reader, false);
            }
            else
            {
                Debug.LogWarning($"没有为 {message.objectId} 的同步消息找到目标。");
            }
        }

        /// <summary>
        /// 客户端场景改变
        /// </summary>
        /// <param name="message"></param>
        private static void OnSceneByClient(SceneMessage message)
        {
            if (isConnect)
            {
                NetworkManager.ClientLoadScene(message.sceneName);
            }
        }
        
        /// <summary>
        /// 客户端未准备就绪的消息 (不能接收和发送消息)
        /// </summary>
        /// <param name="message"></param>
        private static void OnNotReadyByClient(NotReadyMessage message)
        {
            isReady = false;
            OnClientNotReady?.Invoke();
        }
    }
}