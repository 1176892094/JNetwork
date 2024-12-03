// *********************************************************************************
// # Project: Forest
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-12-03  13:12
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace JFramework.Net
{
#if UNITY_EDITOR
    public sealed partial class NetworkObject
    {
        /// <summary>
        /// 自动设置网络对象的唯一标识
        /// </summary>
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
            else if (IsSceneObjectWithPrefabParent(gameObject, out GameObject prefab))
            {
                AssignSceneId();
                AssignAssetPath(AssetDatabase.GetAssetPath(prefab));
            }
            else
            {
                AssignSceneId();
            }
        }

        /// <summary>
        /// 设置资源的路径
        /// </summary>
        /// <param name="path"></param>
        private void AssignAssetPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer == null) return;
                assetId = importer.assetBundleName + "/" + name;
            }
        }

        /// <summary>
        /// 寻找场景物体的父对象
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="prefab"></param>
        /// <returns></returns>
        private static bool IsSceneObjectWithPrefabParent(GameObject gameObject, out GameObject prefab)
        {
            prefab = null;
            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject)) return false;
            prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefab != null) return true;
            Debug.LogError($"找不到场景对象的预制父物体。对象名称：{gameObject.name}");
            return false;
        }

        /// <summary>
        /// 设置对象的场景Id
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void AssignSceneId()
        {
            if (Application.isPlaying) return;
            var duplicate = sceneIds.TryGetValue(sceneId, out NetworkObject @object) && @object != null && @object != this;
            if (sceneId == 0 || duplicate)
            {
                sceneId = 0;
                if (BuildPipeline.isBuildingPlayer)
                {
                    throw new InvalidOperationException($"请构建之前保存场景 {gameObject.scene.path}，场景对象 {name} 没有有效的场景Id。");
                }

                Undo.RecordObject(this, "生成场景Id");
                var randomId = NetworkUtility.GetRandomId();

                duplicate = sceneIds.TryGetValue(randomId, out @object) && @object != null && @object != this;
                if (!duplicate)
                {
                    sceneId = randomId;
                }
            }

            sceneIds[sceneId] = this;
        }
    }
#endif
}