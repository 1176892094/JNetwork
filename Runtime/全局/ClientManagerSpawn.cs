using System.Text;
using UnityEngine;

namespace JFramework.Net
{
    public partial class NetworkManager
    {
        public partial class ClientManager
        {
            /// <summary>
            /// 获取网络对象
            /// </summary>
            /// <param name="message">传入网络消息</param>
            /// <returns>返回是否能获取</returns>
            private async void SpawnObject(SpawnMessage message)
            {
                if (spawns.TryGetValue(message.objectId, out var @object))
                {
                    isSpawning = false;
                    Spawn(message, @object);
                    SpawnFinish();
                    isSpawning = true;
                    return;
                }

                if (message.sceneId == 0)
                {
                    var path = Encoding.UTF8.GetString(message.assetId);
                    var prefab = await GlobalManager.Asset.Load<GameObject>(path);
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
                    Spawn(message, @object);
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
                    Spawn(message, @object);
                    SpawnFinish();
                    isSpawning = true;
                }
            }

            /// <summary>
            /// 生成网络对象
            /// </summary>
            /// <param name="object"></param>
            /// <param name="message"></param>
            private void Spawn(SpawnMessage message, NetworkObject @object)
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
            private void SpawnFinish()
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
        }
    }
}