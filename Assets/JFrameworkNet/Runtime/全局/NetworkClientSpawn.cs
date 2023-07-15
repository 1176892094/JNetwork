using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        /// <summary>
        /// 注册预置体
        /// </summary>
        /// <param name="objects">传入预置体</param>
        private static void RegisterPrefab(List<GameObject> objects)
        {
            foreach (var gameObject in objects.Where(@object => @object != null))
            {
                if (gameObject == null)
                {
                    Debug.LogError("不能注册预置体，因为它是空的。");
                    return;
                }

                if (!gameObject.TryGetComponent(out NetworkObject @object))
                {
                    Debug.LogError($"预置体 {gameObject.name} 没有 NetworkObject 组件");
                    return;
                }

                RegisterPrefab(@object);
            }
        }

        /// <summary>
        /// 注册预置体
        /// </summary>
        /// <param name="object">传入网络对象</param>
        private static void RegisterPrefab(NetworkObject @object)
        {
            if (@object.assetId == 0)
            {
                Debug.LogError($"不能注册预置体 {@object.name} 因为 assetId 为零！");
                return;
            }

            if (@object.sceneId != 0)
            {
                Debug.LogError($"不能注册预置体 {@object.name} 因为 sceneId 不为零");
                return;
            }

            NetworkObject[] identities = @object.GetComponentsInChildren<NetworkObject>();
            if (identities.Length > 1)
            {
                Debug.LogError($"不能注册预置体 {@object.name} 因为它拥有多个 NetworkObject 组件");
            }

            if (prefabs.TryGetValue(@object.assetId, out var gameObject))
            {
                Debug.LogWarning($"旧的预置体 {gameObject.name} 被新的预置体 {@object.name} 所取代。");
            }

            prefabs[@object.assetId] = @object.gameObject;
        }

        /// <summary>
        /// 获取网络对象
        /// </summary>
        /// <param name="event">传入网络事件</param>
        /// <param name="object">输出网络对象</param>
        /// <returns>返回是否能获取</returns>
        private static bool TrySpawn(SpawnEvent @event, out NetworkObject @object)
        {
            if (spawns.TryGetValue(@event.objectId, out @object))
            {
                return true;
            }

            Debug.Log(@event.assetId+"___"+@event.sceneId);
            if (@event is { assetId: 0, sceneId: 0 })
            {
                Debug.LogError($"生成游戏对象 {@event.objectId} 需要保证 assetId 和 sceneId 其中一个不为零");
                return false;
            }

            @object = @event.sceneId == 0 ? SpawnAssetPrefab(@event) : SpawnSceneObject(@event.sceneId);

            if (@object == null)
            {
                Debug.LogError($"不能成 {@object}。 assetId：{@event.assetId} sceneId：{@event.sceneId}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 生成资源预置体
        /// </summary>
        /// <param name="event">传入网络事件</param>
        /// <returns>返回网络对象</returns>
        private static NetworkObject SpawnAssetPrefab(SpawnEvent @event)
        {
            if (prefabs.TryGetValue(@event.assetId, out GameObject prefab))
            {
                var gameObject = Object.Instantiate(prefab, @event.position, @event.rotation);
                return gameObject.GetComponent<NetworkObject>();
            }

            Debug.LogError($"无法生成有效预置体。 assetId：{@event.assetId}  sceneId：{@event.sceneId}");
            return null;
        }

        /// <summary>
        /// 生成场景对象
        /// </summary>
        /// <param name="sceneId">传入场景Id</param>
        /// <returns>返回网络对象</returns>
        private static NetworkObject SpawnSceneObject(ulong sceneId)
        {
            if (scenes.TryGetValue(sceneId, out var @object))
            {
                scenes.Remove(sceneId);
                return @object;
            }

            Debug.LogError($"无法生成有效场景对象。 sceneId：{sceneId}");
            return null;
        }

        /// <summary>
        /// 网络对象生成开始
        /// </summary>
        private static void SpawnStart()
        {
            scenes.Clear();
            var objects = Resources.FindObjectsOfTypeAll<NetworkObject>();
            foreach (var current in objects)
            {
                if (NetworkUtils.IsSceneObject(current) && !current.gameObject.activeSelf)
                {
                    if (scenes.TryGetValue(current.sceneId, out var @object))
                    {
                        var gameObject = current.gameObject;
                        var message = $"复制 {gameObject.name} 到 {@object.gameObject.name} 上检测到sceneId";
                        Debug.LogWarning(message, gameObject);
                    }
                    else
                    {
                        scenes.Add(current.sceneId, current);
                    }
                }
            }
        }

        /// <summary>
        /// 生成网络对象
        /// </summary>
        /// <param name="object"></param>
        /// <param name="event"></param>
        private static void Spawn(NetworkObject @object, SpawnEvent @event)
        {
            if (@event.assetId != 0)
            {
                @object.assetId = @event.assetId;
            }

            if (!@object.gameObject.activeSelf)
            {
                @object.gameObject.SetActive(true);
            }

            @object.objectId = @event.objectId;
            @object.isOwner = @event.isOwner;
            @object.isClient = true;

            var transform = @object.transform;
            transform.localPosition = @event.position;
            transform.localRotation = @event.rotation;
            transform.localScale = @event.localScale;

            if (@event.segment.Count > 0)
            {
                using var reader = NetworkReader.Pop(@event.segment);
                //TODO: @object.DeserializeClient(reader, true);
            }

            spawns[@event.objectId] = @object;

            if (isSpawn)
            {
                @object.OnNotifyAuthority();
                @object.OnStartClient();
            }
        }

        /// <summary>
        /// 网络对象生成结束
        /// </summary>
        private static void SpawnFinish()
        {
            foreach (var @object in spawns.Values.OrderBy(@object => @object.objectId))
            {
                if (@object != null)
                {
                    @object.isClient = true;
                    @object.OnNotifyAuthority();
                    @object.OnStartClient();
                }
                else
                {
                    Debug.LogWarning($"网络对象 {@object} 没有被正确销毁。");
                }
            }
        }

        /// <summary>
        /// 销毁客户端物体
        /// </summary>
        private static void DestroyForClient()
        {
            try
            {
                foreach (var @object in spawns.Values.Where(@object => @object != null && @object.gameObject != null))
                {
                    @object.OnStopClient();
                    if (NetworkManager.mode is NetworkMode.Host or NetworkMode.Client)
                    {
                        if (@object.sceneId != 0)
                        {
                            @object.Reset();
                            @object.gameObject.SetActive(false);
                        }
                        else
                        {
                            Object.Destroy(@object.gameObject);
                        }
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                Debug.LogException(e);
            }
            finally
            {
                spawns.Clear();
            }
        }
    }
}