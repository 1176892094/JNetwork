using System;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JFramework.Net
{
    public static partial class NetworkClient
    {
        /// <summary>
        /// 获取网络对象
        /// </summary>
        /// <param name="message">传入网络消息</param>
        /// <returns>返回是否能获取</returns>
        private static async void SpawnExecute(SpawnMessage message)
        {
            if (spawns.TryGetValue(message.objectId, out var @object))
            {
                isSpawning = false;
                Spawn(@object, message);
                SpawnFinish();
                isSpawning = true;
                return;
            }

            if (message.sceneId == 0)
            {
                var prefab = await GlobalManager.Asset.Load<GameObject>(Encoding.UTF8.GetString(message.assetId));
                if (!prefab.TryGetComponent(out @object))
                {
                    Debug.LogError($"预置体 {prefab.name} 没有 NetworkObject 组件");
                    return;
                }

                if (@object.sceneId != 0)
                {
                    Debug.LogError($"不能注册预置体 {@object.name} 因为 sceneId 不为零");
                    return;
                }

                if (@object.GetComponentsInChildren<NetworkObject>().Length > 1)
                {
                    Debug.LogError($"不能注册预置体 {@object.name} 因为它拥有多个 NetworkObject 组件");
                }

                isSpawning = false;
                Spawn(@object, message);
                SpawnFinish();
                isSpawning = true;
            }
            else
            {
                if (!scenes.TryGetValue(message.sceneId, out @object))
                {
                    Debug.LogError($"无法生成有效场景对象。 sceneId：{message.sceneId}");
                    scenes.Remove(message.sceneId);
                    return;
                }

                scenes.Remove(message.sceneId);
                isSpawning = false;
                Spawn(@object, message);
                SpawnFinish();
                isSpawning = true;
            }
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
            if (isSpawning)
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