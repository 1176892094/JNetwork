using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JFramework.Net
{
    public partial class NetworkManager
    {
        public partial class ServerManager
        {
            /// <summary>
            /// 生成物体
            /// </summary>
            internal void SpawnObjects()
            {
                if (!isActive)
                {
                    Debug.LogError($"服务器不是活跃的。");
                    return;
                }

                var objects = Resources.FindObjectsOfTypeAll<NetworkObject>();
                foreach (var @object in objects)
                {
                    if (NetworkUtils.IsSceneObject(@object) && @object.objectId == 0)
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
            public void Spawn(GameObject obj, NetworkClient client = null)
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

                @object.connection = client;

                if (Instance.mode == NetworkMode.Host)
                {
                    if (@object.connection?.clientId == NetworkConst.HostId)
                    {
                        @object.isOwner = true;
                    }
                }

                if (!@object.isServer && @object.objectId == 0)
                {
                    @object.objectId = ++objectId;
                    @object.isServer = true;
                    @object.isClient = Client.isActive;
                    spawns[@object.objectId] = @object;
                    @object.OnStartServer();
                }

                SpawnForClient(@object);
            }

            /// <summary>
            /// 遍历所有客户端，发送生成物体的消息
            /// </summary>
            /// <param name="object">传入对象</param>
            private void SpawnForClient(NetworkObject @object)
            {
                foreach (var client in clients.Values.Where(client => client.isReady))
                {
                    SendSpawnMessage(client, @object);
                }
            }

            /// <summary>
            /// 服务器向指定客户端发送生成对象的消息
            /// </summary>
            /// <param name="client">指定的客户端</param>
            /// <param name="object">生成的游戏对象</param>
            private void SendSpawnMessage(NetworkClient client, NetworkObject @object)
            {
                using NetworkWriter owner = NetworkWriter.Pop(), observer = NetworkWriter.Pop();
                var isOwner = @object.connection == client;
                var transform = @object.transform;
                var message = new SpawnMessage
                {
                    isOwner = isOwner,
                    sceneId = @object.sceneId,
                    objectId = @object.objectId,
                    position = transform.localPosition,
                    rotation = transform.localRotation,
                    localScale = transform.localScale,
                    segment = SerializeNetworkObject(@object, isOwner, owner, observer),
                    assetId = (ArraySegment<byte>)Encoding.UTF8.GetBytes(@object.assetId)
                };
                client.Send(message);
            }

            /// <summary>
            /// 序列化网络对象，并将数据转发给客户端
            /// </summary>
            /// <param name="object">网络对象生成</param>
            /// <param name="isOwner"></param>
            /// <param name="owner"></param>
            /// <param name="observer"></param>
            /// <returns></returns>
            private ArraySegment<byte> SerializeNetworkObject(NetworkObject @object, bool isOwner, NetworkWriter owner, NetworkWriter observer)
            {
                if (@object.entities.Length == 0) return default;
                @object.ServerSerialize(true, owner, observer);
                return isOwner ? owner.ToArraySegment() : observer.ToArraySegment();
            }

            /// <summary>
            /// 将网络对象重置并隐藏
            /// </summary>
            /// <param name="object"></param>
            public void Despawn(NetworkObject @object)
            {
                spawns.Remove(@object.objectId);
                foreach (var client in clients.Values)
                {
                    Debug.Log($"服务器为客户端 {client.clientId} 重置 {@object}");
                    client.Send(new DespawnMessage(@object.objectId));
                }

                if (Instance.mode == NetworkMode.Host)
                {
                    @object.OnStopClient();
                    @object.isOwner = false;
                    @object.OnNotifyAuthority();
                    Client.spawns.Remove(@object.objectId);
                }

                @object.OnStopServer();
                @object.gameObject.SetActive(false);
                @object.Reset();
            }

            /// <summary>
            /// 将网络对象销毁
            /// </summary>
            /// <param name="object"></param>
            public void Destroy(NetworkObject @object)
            {
                spawns.Remove(@object.objectId);
                @object.isDestroy = true;
                foreach (var client in clients.Values)
                {
                    client.Send(new DestroyMessage(@object.objectId));
                }

                if (Instance.mode == NetworkMode.Host)
                {
                    @object.OnStopClient();
                    @object.isOwner = false;
                    @object.OnNotifyAuthority();
                    Client.spawns.Remove(@object.objectId);
                }

                @object.OnStopServer();
                Destroy(@object.gameObject);
            }
        }
    }
}