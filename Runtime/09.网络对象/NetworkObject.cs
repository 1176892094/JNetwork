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
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkObject : MonoBehaviour
    {
        /// <summary>
        /// 网络变量序列化
        /// </summary>
        internal struct Synchronize
        {
            public int frame;
            public readonly NetworkWriter owner;
            public readonly NetworkWriter observer;
        
            public Synchronize(int frame)
            {
                this.frame = frame;
                owner = new NetworkWriter();
                observer = new NetworkWriter();
            }
        }
        
        /// <summary>
        /// 场景Id列表
        /// </summary>
        private static readonly Dictionary<ulong, NetworkObject> sceneIds = new Dictionary<ulong, NetworkObject>();

        /// <summary>
        /// 上一次序列化间隔
        /// </summary>
        private Synchronize synchronize = new Synchronize(0);

        /// <summary>
        /// 作为资源的路径
        /// </summary>
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.ReadOnly]
#endif
        [SerializeField]
        internal string assetId;

        /// <summary>
        /// 游戏对象Id，用于网络标识
        /// </summary>

#if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.ReadOnly]
#endif
        [SerializeField]
        internal uint objectId;

        /// <summary>
        /// 作为场景资源的Id
        /// </summary>
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.ReadOnly]
#endif
        [SerializeField]
        internal ulong sceneId;

        /// <summary>
        /// 是否有用权限
        /// </summary>
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Sirenix.OdinInspector.ReadOnly]
#endif
        [SerializeField]
        internal ObjectMode objectMode;

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
            isAuthority = false;
            objectMode = ObjectMode.None;
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

        /// <summary>
        /// 服务器帧序列化
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        internal Synchronize Synchronization(int frame)
        {
            if (synchronize.frame != frame)
            {
                synchronize.frame = frame;
                synchronize.owner.position = 0;
                synchronize.observer.position = 0;
                ServerSerialize(false, synchronize.owner, synchronize.observer);
                ClearDirty(true);
            }

            return synchronize;
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
}