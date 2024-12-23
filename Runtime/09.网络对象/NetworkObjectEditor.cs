// *********************************************************************************
// # Project: JFramework
// # Unity: 6000.3.5f1
// # Author: 云谷千羽
// # Version: 1.0.0
// # History: 2024-12-03 13:12:18
// # Recently: 2024-12-22 22:12:00
// # Copyright: 2024, 云谷千羽
// # Description: This is an automatically generated comment.
// *********************************************************************************

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkObject
    {
        private static readonly Dictionary<ulong, NetworkObject> scenes = new Dictionary<ulong, NetworkObject>();
        private static readonly byte[] sourceBuffer = new byte[4];

        private void OnValidate()
        {
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                sceneId = 0;
                AssignAssetPath(AssetDatabase.GetAssetPath(gameObject));
            }
            else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                {
                    sceneId = 0;
                    AssignAssetPath(PrefabStageUtility.GetPrefabStage(gameObject).assetPath);
                }
            }
            else if (IsSceneObjectWithPrefabParent(gameObject, out var prefab))
            {
                AssignSceneId();
                AssignAssetPath(AssetDatabase.GetAssetPath(prefab));
            }
            else
            {
                AssignSceneId();
            }
        }

        private void AssignAssetPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer == null) return;
                var asset = importer.assetBundleName;
                assetId = char.ToUpper(asset[0]) + asset.Substring(1) + "/" + name;
            }
        }

        private static bool IsSceneObjectWithPrefabParent(GameObject gameObject, out GameObject prefab)
        {
            prefab = null;
            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject)) return false;
            prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefab != null) return true;
            Debug.LogError(Service.Text.Format("找不到场景对象的预制父物体。对象名称: {0}", gameObject.name));
            return false;
        }

        private void AssignSceneId()
        {
            if (Application.isPlaying) return;
            var duplicate = scenes.TryGetValue(sceneId, out var @object) && @object != null && @object != this;
            if (sceneId == 0 || duplicate)
            {
                sceneId = 0;
                if (BuildPipeline.isBuildingPlayer)
                {
                    throw new InvalidOperationException("请保存场景后，再进行构建。");
                }

                Undo.RecordObject(this, "生成场景Id");
                Service.Random.NextBytes(sourceBuffer);
                var randomId = MemoryMarshal.Read<uint>(sourceBuffer);

                duplicate = scenes.TryGetValue(randomId, out @object) && @object != null && @object != this;
                if (!duplicate)
                {
                    sceneId = randomId;
                }
            }

            scenes[sceneId] = this;
        }
    }
}
#endif