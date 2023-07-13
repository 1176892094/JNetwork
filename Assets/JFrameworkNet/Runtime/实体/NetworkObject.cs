using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if  UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace JFramework.Net
{
    public sealed partial class NetworkObject : MonoBehaviour
    {
        private static readonly Dictionary<ulong, NetworkObject> sceneIds = new Dictionary<ulong, NetworkObject>();
        
        [ReadOnly, ShowInInspector] public uint netId;
        [ReadOnly, SerializeField] private uint m_assetId;
        [ReadOnly, ShowInInspector] internal ulong sceneId;
        [ReadOnly, ShowInInspector] private ClientEntity m_connection;
        [ReadOnly, ShowInInspector] public bool isOwner;
        [ReadOnly, ShowInInspector] public bool isServer;
        [ReadOnly, ShowInInspector] public bool isClient;
        private bool isStartClient;
        private bool hasAuthority;

        internal uint assetId
        {
            get
            {
#if UNITY_EDITOR
                if (m_assetId == 0)
                {
                    SetupIDs();
                }
#endif
                return m_assetId;
            }
            set
            {
                if (value == 0)
                {
                    Debug.LogError("assetId不能为零");
                    return;
                }
                m_assetId = value;
            }
        }
        internal NetworkEntity[] objects;
        
        public ClientEntity connection
        {
            get => m_connection;
            internal set => m_connection = value;
        }

     

        private void Awake()
        {
            objects = GetComponentsInChildren<NetworkEntity>(true);
        }
        
        private void OnValidate()
        {
#if UNITY_EDITOR
            SetupIDs();
#endif
        }

        /// <summary>
        /// 处理Rpc事件
        /// </summary>
        internal void InvokeRpcEvent(byte index, ushort function, RpcType rpcType, NetworkReader reader, ClientEntity client = null)
        {
            if (this == null)
            {
                Debug.LogWarning($"调用了已经删除的网络对象。{rpcType} [{function}] 网络Id：{netId}");
                return;
            }

            if (index >= objects.Length)
            {
                Debug.LogWarning($"没有找到组件Id：[{index}] 网络Id：{netId}");
                return;
            }

            NetworkEntity invokeComponent = objects[index];
            if (!RpcUtils.Invoke(function, rpcType, reader, invokeComponent, client))
            {
                Debug.LogError($"无法调用{rpcType} [{function}] 网络对象：{gameObject.name} 网络Id：{netId}");
            }
        }

        /// <summary>
        /// 在Server端中序列化
        /// </summary>
        /// <param name="isInit"></param>
        /// <param name="observer"></param>
        internal void SerializeServer(bool isInit,  NetworkWriter observer)
        {
            if (objects == null)
            {
                Debug.LogError($"网络对象持有的 NetworkEntity 为空", gameObject);
                return;
            }

            if (objects.Length > NetworkConst.MaxEntityCount)
            {
                Debug.LogError($"网络对象持有的 NetworkEntity 的数量不能超过{NetworkConst.MaxEntityCount}");
                return;
            }

            NetworkEntity[] entities = objects;

            (ulong ownerMask, ulong observerMask) = ServerDirtyMasks(isInit);
        }

        private (ulong, ulong) ServerDirtyMasks(bool isInit)
        {
            ulong ownerMask = 0;
            ulong observerMask = 0;

            NetworkEntity[] components = objects;
            for (int i = 0; i < components.Length; ++i)
            {
                NetworkEntity component = components[i];

                bool dirty = component.IsDirty();
                ulong nthBit = 1U << i;

                if (isInit || (component.syncDirection == SyncDirection.ServerToClient && dirty))
                {
                    ownerMask |= nthBit;
                }

                if (component.syncMode == SyncMode.Observer && (isInit || dirty))
                {
                    observerMask |= nthBit;
                }
            }

            return (ownerMask, observerMask);
        }

        internal void Reset()
        {
            netId = 0;
            isOwner = false;
            isClient = false;
            isServer = false;
            isStartClient = false;
            hasAuthority = false;
            connection = null;
        }
        
#if UNITY_EDITOR
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