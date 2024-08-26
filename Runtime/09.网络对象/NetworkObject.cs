// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: Charlotte
// # Version: 1.0.0
// # History: 2024-06-05  13:06
// # Copyright: 2024, Charlotte
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JFramework.Interface;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace JFramework.Net
{
    public partial class NetworkObject : MonoBehaviour
    {
        /// <summary>
        /// 网络变量序列化
        /// </summary>
        internal struct NetworkSerialize
        {
            public int frameCount;
            public readonly NetworkWriter owner;
            public readonly NetworkWriter observer;

            public NetworkSerialize(int frameCount)
            {
                owner = new NetworkWriter();
                observer = new NetworkWriter();
                this.frameCount = frameCount;
            }
        }

        /// <summary>
        /// 场景Id列表
        /// </summary>
        private static readonly Dictionary<ulong, NetworkObject> sceneIds = new Dictionary<ulong, NetworkObject>();

        /// <summary>
        /// 上一次序列化间隔
        /// </summary>
        private NetworkSerialize serialize = new NetworkSerialize(0);
        
        /// <summary>
        /// 游戏对象Id，用于网络标识
        /// </summary>
        [SerializeField] internal SpawnMode spawnMode;
        
        /// <summary>
        /// 作为资源的路径
        /// </summary>
        [SerializeField, ReadOnly] internal string assetPath;
        
        /// <summary>
        /// 游戏对象Id，用于网络标识
        /// </summary>
        [SerializeField, ReadOnly] internal uint objectId;

        /// <summary>
        /// 作为场景资源的Id
        /// </summary>
        [SerializeField, ReadOnly] internal ulong sceneId;

        /// <summary>
        /// 是否有用权限
        /// </summary>
        [SerializeField, ReadOnly] internal ObjectMode objectMode;

        /// <summary>
        /// 是否为第一次生成
        /// </summary>
        private bool isSpawn;

        /// <summary>
        /// NetworkManager.Server.Despawn
        /// </summary>
        internal bool isDestroy;

        /// <summary>
        /// 是否经过权限验证
        /// </summary>
        private bool isAuthority;

        /// <summary>
        /// 所持有的 NetworkBehaviour
        /// </summary>
        internal NetworkBehaviour[] entities;

        /// <summary>
        /// 连接的代理
        /// </summary>
        internal NetworkClient connection;

        private void Awake()
        {
            entities = GetComponentsInChildren<NetworkBehaviour>(true);
            if (IsValid())
            {
                for (int i = 0; i < entities.Length; ++i)
                {
                    entities[i].@object = this;
                    entities[i].componentId = (byte)i;
                }
            }
        }

        public void Reset()
        {
            objectId = 0;
            isSpawn = false;
            objectMode = ObjectMode.None;
            isAuthority = false;
            connection = null;
            sceneIds.Clear();
        }

        private void OnDestroy()
        {
            if ((objectMode & ObjectMode.Server) == ObjectMode.Server && !isDestroy)
            {
                NetworkManager.Server.Despawn(gameObject);
            }

            if ((objectMode & ObjectMode.Client) == ObjectMode.Client)
            {
                NetworkManager.Client.spawns.Remove(objectId);
            }
        }

        /// <summary>
        /// 设置为改变
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDirty(ulong mask, int index)
        {
            return (mask & (ulong)(1 << index)) != 0;
        }

        /// <summary>
        /// 判断是否有效
        /// </summary>
        /// <returns></returns>
        private bool IsValid()
        {
            if (entities == null)
            {
                Debug.LogError($"网络对象持有的 NetworkEntity 为空", gameObject);
                return false;
            }

            if (entities.Length > Const.MaxEntity)
            {
                Debug.LogError($"网络对象持有的 NetworkEntity 的数量不能超过{Const.MaxEntity}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 选择性地将需要更新的组件数据序列化并发送给所有者和观察者，以减少不必要的数据传输
        /// </summary>
        /// <param name="start"></param>
        /// <param name="owner"></param>
        /// <param name="observer"></param>
        internal void ServerSerialize(bool start, NetworkWriter owner, NetworkWriter observer)
        {
            var components = entities;
            var (ownerMask, observerMask) = ServerDirtyMasks(start);

            if (ownerMask != 0)
            {
                NetworkUtility.CompressVarUInt(owner, ownerMask);
            }

            if (observerMask != 0)
            {
                NetworkUtility.CompressVarUInt(observer, observerMask);
            }

            if ((ownerMask | observerMask) != 0)
            {
                for (int i = 0; i < components.Length; ++i)
                {
                    var component = components[i];
                    var ownerDirty = IsDirty(ownerMask, i);
                    var observersDirty = IsDirty(observerMask, i);
                    if (ownerDirty || observersDirty)
                    {
                        using var writer = NetworkWriter.Pop();
                        component.Serialize(writer, start);
                        ArraySegment<byte> segment = writer;
                        if (ownerDirty)
                        {
                            owner.WriteBytes(segment.Array, segment.Offset, segment.Count);
                        }

                        if (observersDirty)
                        {
                            observer.WriteBytes(segment.Array, segment.Offset, segment.Count);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 用于指示哪些组件需要被序列化并发送给所有者和观察者
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        private (ulong, ulong) ServerDirtyMasks(bool start)
        {
            ulong ownerMask = 0;
            ulong observerMask = 0;

            var components = entities;
            for (int i = 0; i < components.Length; ++i)
            {
                var component = components[i];
                var dirty = component.IsDirty();
                ulong mask = 1U << i;
                if (start || (component.syncDirection == SyncMode.Server && dirty))
                {
                    ownerMask |= mask;
                }

                if (start || dirty)
                {
                    observerMask |= mask;
                }
            }

            return (ownerMask, observerMask);
        }

        /// <summary>
        /// 服务器反序列化 SyncVar
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal bool ServerDeserialize(NetworkReader reader)
        {
            var components = entities;
            var mask = NetworkUtility.DecompressVarUInt(reader);

            for (int i = 0; i < components.Length; ++i)
            {
                if (IsDirty(mask, i))
                {
                    var component = components[i];

                    if (component.syncDirection == SyncMode.Client)
                    {
                        if (!component.Deserialize(reader, false))
                        {
                            return false;
                        }

                        component.SetSyncVarDirty(ulong.MaxValue);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 客户端反序列化 SyncVar
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="start"></param>
        internal void ClientDeserialize(NetworkReader reader, bool start)
        {
            var components = entities;
            var mask = NetworkUtility.DecompressVarUInt(reader);

            for (int i = 0; i < components.Length; ++i)
            {
                if (IsDirty(mask, i))
                {
                    var component = components[i];
                    component.Deserialize(reader, start);
                }
            }
        }

        /// <summary>
        /// 服务器帧序列化
        /// </summary>
        /// <param name="frameCount"></param>
        /// <returns></returns>
        internal NetworkSerialize ServerSerialize(int frameCount)
        {
            if (serialize.frameCount != frameCount)
            {
                serialize.owner.position = 0;
                serialize.observer.position = 0;
                serialize.frameCount = frameCount;
                ServerSerialize(false, serialize.owner, serialize.observer);
                ClearDirty(true);
            }

            return serialize;
        }

        /// <summary>
        /// 客户端序列化 SyncVar
        /// </summary>
        /// <param name="writer"></param>
        internal void ClientSerialize(NetworkWriter writer)
        {
            var components = entities;
            var dirtyMask = ClientDirtyMask();
            if (dirtyMask != 0)
            {
                NetworkUtility.CompressVarUInt(writer, dirtyMask);
                for (int i = 0; i < components.Length; ++i)
                {
                    var component = components[i];

                    if (IsDirty(dirtyMask, i))
                    {
                        component.Serialize(writer, false);
                    }
                }
            }
        }

        /// <summary>
        /// 客户端改变遮罩
        /// </summary>
        /// <returns></returns>
        private ulong ClientDirtyMask()
        {
            ulong mask = 0;
            var components = entities;
            for (int i = 0; i < components.Length; ++i)
            {
                var component = components[i];
                if ((objectMode & ObjectMode.Owner) == ObjectMode.Owner && component.syncDirection == SyncMode.Client)
                {
                    if (component.IsDirty()) mask |= 1U << i;
                }
            }

            return mask;
        }

        /// <summary>
        /// 清除改变值
        /// </summary>
        /// <param name="total"></param>
        internal void ClearDirty(bool total = false)
        {
            foreach (var entity in entities)
            {
                if (entity.IsDirty() || total)
                {
                    entity.ClearDirty();
                }
            }
        }

        /// <summary>
        /// 处理Rpc事件
        /// </summary>
        internal void InvokeMessage(byte index, ushort function, InvokeMode mode, NetworkReader reader, NetworkClient client = null)
        {
            if (this == null)
            {
                Debug.LogWarning($"调用了已经删除的网络对象。{mode} [{function}] 网络Id：{objectId}");
                return;
            }

            if (index >= entities.Length)
            {
                Debug.LogWarning($"没有找到组件Id：[{index}] 网络Id：{objectId}");
                return;
            }

            if (!NetworkDelegate.Invoke(function, mode, client, reader, entities[index]))
            {
                Debug.LogError($"无法调用{mode} [{function}] 网络对象：{gameObject.name} 网络Id：{objectId}");
            }
        }
    }

    public sealed partial class NetworkObject
    {
        /// <summary>
        /// 仅在客户端调用，当在客户端生成时调用
        /// </summary>
        internal void OnStartClient()
        {
            if (isSpawn) return;
            isSpawn = true;

            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStartClient)?.OnStartClient();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity.gameObject);
                }
            }
        }

        /// <summary>
        /// 仅在客户端调用，当在客户端销毁时调用
        /// </summary>
        internal void OnStopClient()
        {
            if (!isSpawn) return;

            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStopClient)?.OnStopClient();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity.gameObject);
                }
            }
        }

        /// <summary>
        /// 仅在服务器上调用，当在服务器生成时调用
        /// </summary>
        internal void OnStartServer()
        {
            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStartServer)?.OnStartServer();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity.gameObject);
                }
            }
        }

        /// <summary>
        /// 仅在服务器上调用，当在服务器生成时调用
        /// </summary>
        internal void OnStopServer()
        {
            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStopServer)?.OnStopServer();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity.gameObject);
                }
            }
        }

        /// <summary>
        /// 仅在客户端调用，触发Notify则进行权限认证
        /// </summary>
        internal void OnNotifyAuthority()
        {
            if (!isAuthority && (objectMode & ObjectMode.Owner) == ObjectMode.Owner)
            {
                OnStartAuthority();
            }
            else if (isAuthority && (objectMode & ObjectMode.Owner) != ObjectMode.Owner)
            {
                OnStopAuthority();
            }

            isAuthority = (objectMode & ObjectMode.Owner) == ObjectMode.Owner;
        }

        /// <summary>
        /// 仅在客户端调用，当通过验证时调用
        /// </summary>
        private void OnStartAuthority()
        {
            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStartAuthority)?.OnStartAuthority();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity.gameObject);
                }
            }
        }

        /// <summary>
        /// 仅在客户端调用，当停止验证时调用
        /// </summary>
        private void OnStopAuthority()
        {
            foreach (var entity in entities)
            {
                try
                {
                    (entity as IStopAuthority)?.OnStopAuthority();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, entity.gameObject);
                }
            }
        }
    }

#if UNITY_EDITOR
    public sealed partial class NetworkObject
    {
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

        private void AssignAssetPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer == null) return;
                assetPath = importer.assetBundleName + "/" + name;
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