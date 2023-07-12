using System;
using System.Linq;
using UnityEngine;

namespace JFramework.Net
{
    public static partial class ServerManager
    {
        /// <summary>
        /// 服务器给指定客户端生成游戏对象
        /// </summary>
        /// <param name="client">传入指定客户端</param>
        private static void SpawnForClient(ClientEntity client)
        {
            if (!client.isReady) return;
            Debug.Log($"客户端 {client.clientId} 开始生成物体");
            foreach (var @object in spawns.Values)
            {
                if (@object.gameObject.activeSelf)
                {
                    client.AddObserver(@object);
                }
            }
        }

        /// <summary>
        /// 服务器给指定客户端移除游戏对象
        /// </summary>
        /// <param name="client">传入指定客户端</param>
        /// <param name="object">传入指定对象</param>
        internal static void DespawnForClient(ClientEntity client, NetworkObject @object)
        {
            DespawnEvent @event = new DespawnEvent
            {
                netId = @object.netId
            };
            client.Send(@event);
        }


        /// <summary>
        /// 服务器向指定客户端发送生成对象的消息
        /// </summary>
        /// <param name="client">指定的客户端</param>
        /// <param name="object">生成的游戏对象</param>
        internal static void SendSpawnMessage(ClientEntity client, NetworkObject @object)
        {
            Debug.Log($"服务器为客户端 {client.clientId} 生成 {@object}");
            using NetworkWriter owner = NetworkWriter.Pop(), observer = NetworkWriter.Pop();
            bool isOwner = @object.connection == client;
            ArraySegment<byte> segment = SerializeNetworkObject(@object, isOwner, owner, observer);
            var transform = @object.transform;
            SpawnEvent message = new SpawnEvent
            {
                netId = @object.netId,
                sceneId = @object.sceneId,
                assetId = @object.assetId,
                position = transform.localPosition,
                rotation = transform.localRotation,
                localScale = transform.localScale,
                isOwner = @object.connection == client,
                segment = segment
            };
            client.Send(message);
        }

        /// <summary>
        /// 序列化网络对象，并将数据转发给客户端
        /// </summary>
        /// <param name="object">网络对象生成</param>
        /// <param name="isOwner">是否包含权限</param>
        /// <param name="owner">有权限的</param>
        /// <param name="observer"></param>
        /// <returns></returns>
        private static ArraySegment<byte> SerializeNetworkObject(NetworkObject @object, bool isOwner, NetworkWriter owner, NetworkWriter observer)
        {
            if (@object.objects.Length == 0) return default;
            @object.SerializeServer(true, owner, observer);
            ArraySegment<byte> segment = isOwner ? owner.ToArraySegment() : observer.ToArraySegment();
            return segment;
        }

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
                if (NetworkUtils.IsSceneObject(@object) && @object.netId == 0)
                {
                    @object.gameObject.SetActive(true);
                    if (NetworkUtils.IsValidParent(@object))
                    {
                        Spawn(@object.gameObject, @object.connection);
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

            if (spawns.ContainsKey(@object.netId))
            {
                Debug.LogWarning($"网络对象 {@object} 已经被生成。", @object.gameObject);
                return;
            }
            
            @object.m_connection = client;
            
            if (isHost)
            {
                @object.isOwner = true;
            }
            
            if (!@object.isServer && @object.netId == 0)
            {
                @object.netId = ++netId;
                @object.isServer = true;
                @object.isClient = ClientManager.isActive;
                spawns[@object.netId] = @object;
                @object.OnStartServer();
            }
            
            Rebuild(@object);
        }

        /// <summary>
        /// 重新构建对象的观察连接
        /// </summary>
        /// <param name="object">传入对象</param>
        private static void Rebuild(NetworkObject @object)
        {
            foreach (var client in clients.Values.Where(client => client.isReady))
            {
                client.AddObserver(@object);
            }
          
            if (connection is { isReady: true })
            {
                connection.AddObserver(@object);
            }
        }
    }
}