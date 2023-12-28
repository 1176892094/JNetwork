using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JFramework.Net
{
    public partial class NetworkManager
    {
        public partial class NetworkClient
        {
            /// <summary>
            /// 注册网络消息
            /// </summary>
            /// <param name="isHost">是否是基于主机的连接</param>
            private void RegisterMessage(bool isHost)
            {
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
                    RegisterMessage<PongMessage>(NetworkTime.OnPongByClient);
                    RegisterMessage<EntityMessage>(OnEntityEvent);
                }

                RegisterMessage<NotReadyMessage>(OnNotReadyByClient);
                RegisterMessage<SceneMessage>(OnSceneByClient);
                RegisterMessage<SnapshotMessage>(OnSnapshotByClient);
                RegisterMessage<InvokeRpcMessage>(OnInvokeRpcByClient);
            }

            /// <summary>
            /// 注册网络消息
            /// </summary>
            private void RegisterMessage<TMessage>(Action<TMessage> handle) where TMessage : struct, Message
            {
                messages[NetworkMessage<TMessage>.Id] = NetworkMessage.Register(handle);
            }

            /// <summary>
            /// 主机模式下空的网络消息
            /// </summary>
            /// <param name="message"></param>
            /// <typeparam name="T"></typeparam>
            private void OnEmptyByHost<T>(T message) where T : Message
            {
            }

            /// <summary>
            /// 主机模式下生成物体的消息
            /// </summary>
            /// <param name="message"></param>
            private void OnSpawnByHost(SpawnMessage message)
            {
                if (Server.spawns.TryGetValue(message.objectId, out var @object))
                {
                    spawns[message.objectId] = @object;
                    @object.gameObject.SetActive(true);
                    @object.isOwner = message.isOwner;
                    @object.isClient = true;
                    @object.OnStartClient();
                    @object.OnNotifyAuthority();
                }
            }

            /// <summary>
            /// 客户端下隐藏物体的消息
            /// </summary>
            /// <param name="message"></param>
            private void OnDespawnByClient(DespawnMessage message)
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
            private void OnDestroyByClient(DestroyMessage message)
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
            private void OnSpawnByClient(SpawnMessage message)
            {
                scenes.Clear();
                var objects = Resources.FindObjectsOfTypeAll<NetworkObject>();
                foreach (var obj in objects)
                {
                    if (!NetworkUtils.IsSceneObject(obj)) continue;
                    if (scenes.TryGetValue(obj.sceneId, out var o))
                    {
                        var gameObject = obj.gameObject;
                        Debug.LogWarning($"复制 {gameObject.name} 到 {o.gameObject.name} 上检测到 sceneId", gameObject);
                    }
                    else
                    {
                        scenes.Add(obj.sceneId, obj);
                    }
                }

                SpawnExecute(message);
            }

            /// <summary>
            /// 接收 远程过程调用(RPC) 缓存的消息
            /// </summary>
            /// <param name="message"></param>
            private void OnInvokeRpcByClient(InvokeRpcMessage message)
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
            private void OnClientRpcEvent(ClientRpcMessage message)
            {
                if (!spawns.TryGetValue(message.objectId, out var @object)) return;
                using var reader = NetworkReader.Pop(message.segment);
                @object.InvokeRpcMessage(message.serialId, message.methodHash, RpcType.ClientRpc, reader);
            }

            /// <summary>
            /// 客户端下网络消息快照的消息
            /// </summary>
            /// <param name="message"></param>
            private void OnSnapshotByClient(SnapshotMessage message)
            {
                connection.OnSnapshotMessage(new SnapshotTime(connection.remoteTime, NetworkTime.localTime));
            }

            /// <summary>
            /// 实体状态同步
            /// </summary>
            /// <param name="message"></param>
            private void OnEntityEvent(EntityMessage message)
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
            private void OnSceneByClient(SceneMessage message)
            {
                if (isConnect)
                {
                    Instance.ClientLoadScene(message.sceneName);
                }
            }

            /// <summary>
            /// 客户端未准备就绪的消息 (不能接收和发送消息)
            /// </summary>
            /// <param name="message"></param>
            private void OnNotReadyByClient(NotReadyMessage message)
            {
                isReady = false;
                OnClientNotReady?.Invoke();
            }
        }
    }
}