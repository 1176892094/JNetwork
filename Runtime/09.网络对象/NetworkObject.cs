// *********************************************************************************
// # Project: JFramework
// # Unity: 6000.3.5f1
// # Author: 云谷千羽
// # Version: 1.0.0
// # History: 2024-12-21 23:12:50
// # Recently: 2024-12-22 23:12:53
// # Copyright: 2024, 云谷千羽
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public sealed partial class NetworkObject : MonoBehaviour, IEntity
    {
        [SerializeField] internal string assetId;

        [SerializeField] internal uint objectId;

        [SerializeField] internal ulong sceneId;

        [SerializeField] internal EntityMode entityMode;

        internal NetworkClient connection;

        internal NetworkBehaviour[] entities;

        internal EntityState entityState;

        private NetworkSerialize serialize = new NetworkSerialize(0);

        private void Awake()
        {
            entities = GetComponentsInChildren<NetworkBehaviour>(true);
            if (IsValid())
            {
                for (var i = 0; i < entities.Length; ++i)
                {
                    entities[i].@object = this;
                    entities[i].componentId = (byte)i;
                }
            }
        }

        public void Reset()
        {
            objectId = 0;
            connection = null;
            entityMode = EntityMode.None;
            entityState = EntityState.None;
        }

        private void OnDestroy()
        {
            if ((entityMode & EntityMode.Server) == EntityMode.Server && (entityState & EntityState.Destroy) == 0)
            {
                NetworkManager.Server.Despawn(gameObject);
            }

            if ((entityMode & EntityMode.Client) != 0)
            {
                NetworkManager.Client.spawns.Remove(objectId);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDirty(ulong mask, int index)
        {
            return (mask & (ulong)(1 << index)) != 0;
        }

        private bool IsValid()
        {
            if (entities == null)
            {
                Debug.LogError("网络对象持有的 NetworkEntity 为空", gameObject);
                return false;
            }

            if (entities.Length > 64)
            {
                Debug.LogError("网络对象持有的 NetworkEntity 的数量不能超过 64");
                return false;
            }

            return true;
        }

        internal void InvokeMessage(byte index, ushort function, InvokeMode mode, MemoryReader reader, NetworkClient client = null)
        {
            if (this == null)
            {
                Debug.LogWarning(Service.Text.Format("调用了已经删除的网络对象。{0} [{1}] {2}", mode, function, objectId));
                return;
            }

            if (index >= entities.Length)
            {
                Debug.LogWarning(Service.Text.Format("网络对象{0}，没有找到组件{1}", objectId, index));
                return;
            }

            if (!NetworkDelegate.Invoke(function, mode, client, reader, entities[index]))
            {
                Debug.LogError(Service.Text.Format("无法调用{0} [{1}] 网络对象: {2} 网络标识: {3}", mode, function, gameObject.name, objectId));
            }
        }

        internal NetworkSerialize Synchronization(int frame)
        {
            if (serialize.frame != frame)
            {
                serialize.frame = frame;
                serialize.owner.position = 0;
                serialize.observer.position = 0;
                ServerSerialize(false, serialize.owner, serialize.observer);
                ClearDirty(true);
            }

            return serialize;
        }

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

        internal void OnStartClient()
        {
            if ((entityState & EntityState.Spawn) != 0)
            {
                return;
            }

            entityState |= EntityState.Spawn;

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

        internal void OnStopClient()
        {
            if ((entityState & EntityState.Spawn) == 0)
            {
                return;
            }

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

        internal void OnNotifyAuthority()
        {
            if ((entityState & EntityState.Authority) == 0 && (entityMode & EntityMode.Owner) != 0)
            {
                OnStartAuthority();
            }
            else if ((entityState & EntityState.Authority) != 0 && (entityMode & EntityMode.Owner) == 0)
            {
                OnStopAuthority();
            }

            if ((entityMode & EntityMode.Owner) != 0)
            {
                entityState |= EntityState.Authority;
            }
            else
            {
                entityState &= ~EntityState.Authority;
            }
        }

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

        internal struct NetworkSerialize
        {
            public int frame;
            public readonly MemoryWriter owner;
            public readonly MemoryWriter observer;

            public NetworkSerialize(int frame)
            {
                this.frame = frame;
                owner = new MemoryWriter();
                observer = new MemoryWriter();
            }
        }
    }
}