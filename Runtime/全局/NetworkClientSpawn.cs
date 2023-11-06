using System;
using System.Linq;
using System.Text;
using JFramework.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        /// <summary>
        /// 网络对象生成开始 (标记场景中的NetworkObject)
        /// </summary>
        private static void SpawnStart()
        {
            scenes.Clear();
            var objects = Resources.FindObjectsOfTypeAll<NetworkObject>();
            foreach (var @object in objects)
            {
                if (NetworkUtils.IsSceneObject(@object))
                {
                    if (scenes.TryGetValue(@object.sceneId, out var sceneObject))
                    {
                        var gameObject = @object.gameObject;
                        Debug.LogWarning($"复制 {gameObject.name} 到 {sceneObject.gameObject.name} 上检测到 sceneId", gameObject);
                    }
                    else
                    {
                        scenes.Add(@object.sceneId, @object);
                    }
                }
            }
        }

        /// <summary>
        /// 获取网络对象
        /// </summary>
        /// <param name="message">传入网络消息</param>
        /// <param name="object">输出网络对象</param>
        /// <returns>返回是否能获取</returns>
        private static bool TrySpawn(SpawnMessage message, out NetworkObject @object)
        {
            if (spawns.TryGetValue(message.objectId, out @object))
            {
                return true;
            }

            @object = message.sceneId == 0 ? SpawnPrefab(message) : SpawnSceneObject(message);

            if (@object == null)
            {
                Debug.LogError($"无法获取网络组件 {@object}。 sceneId：{message.sceneId}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 生成网络预置体
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static NetworkObject SpawnPrefab(SpawnMessage message)
        {
            var prefab = AssetManager.Load<GameObject>(Encoding.UTF8.GetString(message.assetId));
            if (!prefab.TryGetComponent(out NetworkObject @object))
            {
                Debug.LogError($"预置体 {prefab.name} 没有 NetworkObject 组件");
                return null;
            }

            if (@object.sceneId != 0)
            {
                Debug.LogError($"不能注册预置体 {@object.name} 因为 sceneId 不为零");
                return null;
            }

            if (@object.GetComponentsInChildren<NetworkObject>().Length > 1)
            {
                Debug.LogError($"不能注册预置体 {@object.name} 因为它拥有多个 NetworkObject 组件");
                return null;
            }

            var transform = prefab.transform;
            transform.position = message.position;
            transform.rotation = message.rotation;
            transform.localScale = message.localScale;
            return @object;
        }

        /// <summary>
        /// 生成网络场景对象
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static NetworkObject SpawnSceneObject(SpawnMessage message)
        {
            NetworkObject @object = null;

            if (message.sceneId != 0 && !scenes.TryGetValue(message.sceneId, out @object))
            {
                Debug.LogError($"无法生成有效场景对象。 sceneId：{message.sceneId}");
                scenes.Remove(message.sceneId);
                return null;
            }

            scenes.Remove(message.sceneId);
            return @object;
        }

        /// <summary>
        /// 生成网络对象
        /// </summary>
        /// <param name="object"></param>
        /// <param name="message"></param>
        private static void Spawn(NetworkObject @object, SpawnMessage message)
        {
            if (!@object.gameObject.activeSelf)
            {
                @object.gameObject.SetActive(true);
            }

            @object.assetId = Encoding.UTF8.GetString(message.assetId);
            @object.objectId = message.objectId;
            @object.isOwner = message.isOwner;
            @object.isClient = true;

            var transform = @object.transform;
            transform.localPosition = message.position;
            transform.localRotation = message.rotation;
            transform.localScale = message.localScale;

            if (message.segment.Count > 0)
            {
                using var reader = NetworkReader.Pop(message.segment);
                @object.ClientDeserialize(reader, true);
            }

            spawns[message.objectId] = @object;
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
            foreach (var @object in spawns.Values)
            {
                if (@object == null)
                {
                    Debug.LogWarning($"网络对象 {@object} 没有被正确销毁。");
                    continue;
                }

                @object.isClient = true;
                @object.OnNotifyAuthority();
                @object.OnStartClient();
            }
        }

        /// <summary>
        /// 销毁客户端物体
        /// </summary>
        private static void DestroyForClient()
        {
            try
            {
                var enumerable = spawns.Values.Where(@object => @object != null);
                foreach (var @object in enumerable)
                {
                    @object.OnStopClient();
                    if (NetworkManager.mode is NetworkMode.Client)
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