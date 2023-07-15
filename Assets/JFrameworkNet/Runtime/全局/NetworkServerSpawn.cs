using System;
using System.Linq;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class NetworkServer
    {
        /// <summary>
        /// 生成物体
        /// </summary>
        internal static void SpawnObjects()
        {
            if (!isActive)
            {
                Debug.LogError($"服务器不是活跃的。");
                return;
            }
            
            NetworkObject[] objects = Resources.FindObjectsOfTypeAll<NetworkObject>();

            foreach (var @object in objects)
            {
                if (NetworkUtils.IsSceneObject(@object) && @object.objectId == 0)
                {
                    @object.gameObject.SetActive(true);
                    if (NetworkUtils.IsValidParent(@object))
                    {
                        Spawn(@object.gameObject, @object.client);
                    }
                }
            }
        }
        
        /// <summary>
        /// 仅在Server和Host能使用，生成物体的方法
        /// </summary>
        /// <param name="obj">生成的游戏物体</param>
        /// <param name="client">客户端Id</param>
        public static void Spawn(GameObject obj, ClientEntity client = null)
        {
            if (!isActive)
            {
                Debug.LogError($"服务器不是活跃的。", obj);
                return;
            }

            if (!obj.TryGetComponent(out NetworkObject @object))
            {
                Debug.LogError($"生成对象 {obj} 没有 NetworkObject 组件", obj);
                return;
            }

            if (spawns.ContainsKey(@object.objectId))
            {
                Debug.LogWarning($"网络对象 {@object} 已经被生成。", @object.gameObject);
                return;
            }
            
            @object.client = client;
            
            if (NetworkManager.mode == NetworkMode.Host)
            {
                @object.isOwner = true;
            }
            
            if (!@object.isServer && @object.objectId == 0)
            {
                @object.objectId = ++objectId;
                @object.isServer = true;
                @object.isClient = NetworkClient.isActive;
                spawns[@object.objectId] = @object;
                @object.OnStartServer();
            }
            
            SpawnForClient(@object);
        }

        /// <summary>
        /// 遍历所有客户端，发送生成物体的事件
        /// </summary>
        /// <param name="object">传入对象</param>
        private static void SpawnForClient(NetworkObject @object)
        {
            foreach (var client in clients.Values.Where(client => client.isReady))
            {
                SendSpawnEvent(client, @object);
            }
        }

        /// <summary>
        /// 服务器向指定客户端发送生成对象的消息
        /// </summary>
        /// <param name="client">指定的客户端</param>
        /// <param name="object">生成的游戏对象</param>
        private static void SendSpawnEvent(ClientEntity client, NetworkObject @object)
        {
            Debug.Log($"服务器为客户端 {client.clientId} 生成 {@object}");
            using NetworkWriter owner = NetworkWriter.Pop(), observer = NetworkWriter.Pop();
            var isOwner = @object.client == client;
            var transform = @object.transform;
            var @event = new SpawnEvent
            {
                isOwner = isOwner,
                sceneId = @object.sceneId,
                assetId = @object.assetId,
                objectId = @object.objectId,
                position = transform.localPosition,
                rotation = transform.localRotation,
                localScale = transform.localScale,
                segment = SerializeNetworkObject(@object, isOwner, owner, observer)
            };
            client.Send(@event);
        }

        /// <summary>
        /// 序列化网络对象，并将数据转发给客户端
        /// </summary>
        /// <param name="object">网络对象生成</param>
        /// <param name="isOwner"></param>
        /// <param name="owner"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        private static ArraySegment<byte> SerializeNetworkObject(NetworkObject @object, bool isOwner, NetworkWriter owner, NetworkWriter observer)
        {
            if (@object.entities.Length == 0) return default;
            @object.SerializeServer(true, owner, observer);
            return isOwner ? owner.ToArraySegment() : observer.ToArraySegment();
        }

        /// <summary>
        /// 将网络对象重置并隐藏
        /// </summary>
        /// <param name="object"></param>
        public static void Despawn(NetworkObject @object)
        {
            spawns.Remove(@object.objectId);

            if (NetworkManager.mode == NetworkMode.Host)
            {
                @object.isOwner = false;
                @object.OnStopClient();
                @object.OnNotifyAuthority();
                NetworkClient.spawns.Remove(@object.objectId);
            }

            @object.OnStopServer();
            @object.Reset();
            DespawnForClient(@object);
        }

        /// <summary>
        /// 向所有客户端发送对象重置的消息
        /// </summary>
        /// <param name="object"></param>
        private static void DespawnForClient(NetworkObject @object)
        {
            foreach (var client in clients.Values)
            {
                SendDespawnEvent(client, @object);
            }
        }
        
        /// <summary>
        /// 服务器给指定客户端移除游戏对象
        /// </summary>
        /// <param name="client">传入指定客户端</param>
        /// <param name="object">传入指定对象</param>
        private static void SendDespawnEvent(ClientEntity client, NetworkObject @object)
        {
            Debug.Log($"服务器为客户端 {client.clientId} 重置 {@object}");
            client.Send(new DespawnEvent(@object.objectId));
        }
    }
}