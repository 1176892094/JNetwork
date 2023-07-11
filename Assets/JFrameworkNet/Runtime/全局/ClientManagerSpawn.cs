using UnityEngine;

namespace JFramework.Net
{
    public static partial class ClientManager
    {
        internal static void RegisterPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Could not register prefab because it was null");
                return;
            }

            if (!prefab.TryGetComponent(out NetworkObject @object))
            {
                Debug.LogError($"{prefab.name} is not NetworkIdentity component");
                return;
            }

            RegisterPrefab(@object);
        }

        private static void RegisterPrefab(NetworkObject @object)
        {
            if (@object.assetId == 0)
            {
                Debug.LogError($"Can not Register '{@object.name}' because it had empty assetId");
                return;
            }

            if (@object.sceneId != 0)
            {
                Debug.LogError($"Can not Register '{@object.name}' because it has a sceneId");
                return;
            }

            NetworkObject[] identities = @object.GetComponentsInChildren<NetworkObject>();
            if (identities.Length > 1)
            {
                Debug.LogError($"Prefab '{@object.name}' has multiple NetworkIdentity components.");
            }

            if (prefabs.TryGetValue(@object.assetId, out var gameObject))
            {
                Debug.LogWarning($"Replacing existing prefab with assetId {gameObject.name} --> {@object.name}");
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
                Debug.LogError($"Spawn message with netId {@event.netId} has no assetId and sceneId");
                return false;
            }

            @object = @event.sceneId == 0 ? SpawnAssetPrefab(@event) : SpawnSceneObject(@event.sceneId);

            if (@object == null)
            {
                Debug.LogError($"Could not spawn NetworkObject assetId = {@event.netId} sceneId = {@event.sceneId}");
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
            
            Debug.LogError($"Spawn prefab not found for assetId = {@event.assetId}");
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

            Debug.LogError($"Spawn scene object not found for {sceneId}");
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