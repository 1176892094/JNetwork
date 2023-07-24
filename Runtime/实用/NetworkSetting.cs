using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JFramework.Net
{
    internal class NetworkSetting : ScriptableObject
    {
        [SerializeField] internal List<GameObject> prefabs = new List<GameObject>();

        
        [InfoBox("本地模拟相对于发送间隔 * 缓冲时间乘数 的滞后时间")]
        public double bufferTimeMultiplier = 2;

        [InfoBox("当本地时间线快速朝向远程时间时，减速开始")]
        public float catchupNegativeThreshold = -1;
        
        [InfoBox("当本地时间线移动太慢，距离远程时间太远时，开始追赶")]
        public float catchupPositiveThreshold = 1;
        
        [InfoBox("在追赶时本地时间线的加速百分比")]
        [Range(0, 1)] public double catchupSpeed = 0.02f;
        
        [InfoBox("在减速时本地时间线的减速百分比")]
        [Range(0, 1)] public double slowdownSpeed = 0.04f;
        
        [InfoBox("追赶/减速通过 n 秒的指数移动平均调整")]
        public int driftEmaDuration = 1;
        
        [InfoBox("自动调整 bufferTimeMultiplier 以获得平滑结果")]
        public bool dynamicAdjustment = true;
        
        [InfoBox("动态调整时始终添加到 bufferTimeMultiplier 的安全缓冲")]
        public float dynamicAdjustmentTolerance = 1;
        
        [InfoBox("动态调整通过 n 秒的指数移动平均标准差计算")]
        public int deliveryTimeEmaDuration = 2;

#if UNITY_EDITOR
        /// <summary>
        /// 单例自身
        /// </summary>
        private static NetworkSetting instance;
        
        /// <summary>
        /// 获取创建或寻找单例
        /// </summary>
        private static NetworkSetting Instance
        {
            get
            {
                if (instance != null) return instance;
                const string path = "Assets/AddressableResources/Settings";
                var asset = $"{path}/{nameof(NetworkSetting)}.asset";
                instance = AssetDatabase.LoadAssetAtPath<NetworkSetting>(asset);
                if (instance != null) return instance;
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                instance = CreateInstance<NetworkSetting>();
                AssetDatabase.CreateAsset(instance, asset);
                AssetDatabase.Refresh();
                Debug.Log($"创建 <color=#00FF00>{nameof(NetworkSetting)}</color> 单例资源。路径: <color=#FFFF00>{path}</color>");
                return instance;
            }
        }

        private void OnValidate()
        {
            bufferTimeMultiplier = 2;
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
        }
#endif
    }
}