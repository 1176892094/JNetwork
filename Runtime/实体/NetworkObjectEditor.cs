using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace JFramework.Net
{
    public sealed partial class NetworkObject
    {
#if UNITY_EDITOR
        private void OnValidate()
        {
            SetupIDs();
        }

        private void SetupIDs()
        {
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                sceneId = 0;
                AssignAssetID(AssetDatabase.GetAssetPath(gameObject));
            }
            else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                {
                    sceneId = 0;
                    AssignAssetID(PrefabStageUtility.GetPrefabStage(gameObject).assetPath);
                }
            }
            else if (IsSceneObjectWithPrefabParent(gameObject, out GameObject prefab))
            {
                AssignSceneID();
                AssignAssetID(AssetDatabase.GetAssetPath(prefab));
            }
            else
            {
                AssignSceneID();
            }
        }

        private void AssignAssetID(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                Guid guid = new Guid(AssetDatabase.AssetPathToGUID(path));
                assetId = (uint)guid.GetHashCode();
            }
        }

        private static bool IsSceneObjectWithPrefabParent(GameObject gameObject, out GameObject prefab)
        {
            prefab = null;
            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject)) return false;
            prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefab != null) return true;
            Debug.LogError($"找不到场景对象的预制父物体。对象名称：{gameObject.name}");
            return false;
        }

        private void AssignSceneID()
        {
            if (Application.isPlaying) return;
            bool duplicate = sceneIds.TryGetValue(sceneId, out NetworkObject @object) && @object != null && @object != this;
            if (sceneId == 0 || duplicate)
            {
                sceneId = 0;
                if (BuildPipeline.isBuildingPlayer)
                {
                    throw new InvalidOperationException($"请构建之前保存场景 {gameObject.scene.path}，场景对象 {name} 没有有效的场景Id。");
                }

                Undo.RecordObject(this, "生成场景Id");

                uint randomId = (uint)NetworkUtils.GenerateRandom();

                duplicate = sceneIds.TryGetValue(randomId, out @object) && @object != null && @object != this;
                if (!duplicate)
                {
                    sceneId = randomId;
                }
            }

            sceneIds[sceneId] = this;
        }
#endif
    }
}