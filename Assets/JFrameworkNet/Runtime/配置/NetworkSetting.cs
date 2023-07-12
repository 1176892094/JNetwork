using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JFramework.Net
{
    [CreateAssetMenu(fileName = "NetworkSetting", menuName = "Network/NetworkSetting")]
    internal class NetworkSetting : ScriptableObject
    {
        public List<GameObject> objectPrefabs = new List<GameObject>();

        /// <summary>
        /// 自动查找所有的NetworkObject
        /// </summary>
        private void OnValidate()
        {
            objectPrefabs.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<NetworkObject>() != null)
                {
                    objectPrefabs.Add(prefab);
                }
            }
        }

        /// <summary>
        /// 注册预置体到客户端
        /// </summary>
        public void RegisterPrefab()
        {
            foreach (var gameObject in objectPrefabs.Where(gameObject => gameObject != null))
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

                ClientManager.RegisterPrefab(@object);
            }
        }
    }
}