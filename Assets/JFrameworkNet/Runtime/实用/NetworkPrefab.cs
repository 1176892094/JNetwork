using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JFramework.Net
{
    public class NetworkPrefab : ScriptableObject
    {
        /// <summary>
        /// 单例自身
        /// </summary>
        private static NetworkPrefab instance;

        /// <summary>
        /// 预置体列表
        /// </summary>
        [SerializeField] internal List<GameObject> prefabs = new List<GameObject>();

#if UNITY_EDITOR
        /// <summary>
        /// 获取创建或寻找单例
        /// </summary>
        public static NetworkPrefab Instance
        {
            get
            {
                if (instance != null) return instance;
                const string path = "Assets/AddressableResources/Settings";
                var asset = $"{path}/{nameof(NetworkPrefab)}.asset";
                instance = AssetDatabase.LoadAssetAtPath<NetworkPrefab>(asset);
                if (instance != null) return instance;
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                instance = CreateInstance<NetworkPrefab>();
                AssetDatabase.CreateAsset(instance, asset);
                AssetDatabase.Refresh();
                Debug.Log($"创建 <color=#00FF00>{nameof(NetworkPrefab)}</color> 单例资源。路径: <color=#FFFF00>{path}</color>");
                return instance;
            }
        }

        /// <summary>
        /// 自动寻找预置体
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad() => Instance.FindPrefabs();

        /// <summary>
        /// 寻找预置体的方法
        /// </summary>
        private void FindPrefabs()
        {
            prefabs.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<NetworkObject>() != null)
                {
                    prefabs.Add(prefab);
                }
            }
#endif
        }
    }
}