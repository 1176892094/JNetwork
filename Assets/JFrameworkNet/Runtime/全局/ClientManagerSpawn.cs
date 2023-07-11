using UnityEngine;

namespace JFramework.Net
{
    public static partial class ClientManager
    {
        /// <summary>
        /// 注册预置体
        /// </summary>
        /// <param name="prefab">传入预置体</param>
        internal static void RegisterPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("不能注册预置体，因为它是空的。");
                return;
            }

            if (!prefab.TryGetComponent(out NetworkObject @object))
            {
                Debug.LogError($"预置体 {prefab.name} 没有 NetworkObject 组件");
                return;
            }

            RegisterPrefab(@object);
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
                Debug.LogError($"不能注册预置体 {@object.name} 因为它挂在了多个 NetworkObject 组件");
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
            if (spawns.TryGetValue(@event.netId, out @object))
            {
                return true;
            }

            if (@event is { assetId: 0, sceneId: 0 })
            {
                Debug.LogError($"生成游戏对象 {@event.netId} 需要保证 assetId 和 sceneId 其中一个不为零");
                return false;
            }

            @object = @event.sceneId == 0 ? SpawnAssetPrefab(@event) : SpawnSceneObject(@event.sceneId);

            if (@object == null)
            {
                Debug.LogError($"不能成 {@object}。 assetId：{@event.netId} sceneId：{@event.sceneId}");
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

            @object.netId = @event.netId;
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

            spawns[@event.netId] = @object;
            if (@object.isOwner)
            {
                connection?.observers.Add(@object);
            }
            
            if (isSpawn)
            {
                @object.OnNotifyAuthority();
                @object.OnStartClient();
            }
        }
    }
}