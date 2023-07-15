using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public abstract partial class NetworkEntity
    {
#region General

        /// <summary>
        /// 添加标准的标准变量设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GeneralSyncVarSetter<T>(T value, ref T field, ulong dirty, Action<T, T> OnChanged)
        {
            if (!GeneralSyncVarEqual(value, ref field))
            {
                T oldValue = field;
                SetGeneralSyncVar(value, ref field, dirty);
                if (OnChanged != null)
                {
                    if (NetworkManager.mode == NetworkMode.Host && !GetSyncVarHook(dirty))
                    {
                        SetSyncVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetSyncVarHook(dirty, false);
                    }
                }
            }
        }
        
        /// <summary>
        /// 添加标准网络变量的访问器
        /// </summary>
        /// <param name="field"></param>
        /// <param name="OnChanged"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GeneralSyncVarGetter<T>(ref T field, Action<T, T> OnChanged, T value)
        {
            T oldValue = field;
            field = value;
            if (OnChanged != null && !GeneralSyncVarEqual(oldValue, ref field))
            {
                OnChanged(oldValue, field);
            }
        }
        
        /// <summary>
        /// 标准网络变量的值比较器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static bool GeneralSyncVarEqual<T>(T value, ref T field)
        {
            return EqualityComparer<T>.Default.Equals(value, field);
        }
        
        /// <summary>
        /// 标准网络变量的设置
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <typeparam name="T"></typeparam>
        // ReSharper disable once RedundantAssignment
        private void SetGeneralSyncVar<T>(T value, ref T field, ulong dirty)
        {
            SetSyncVarDirty(dirty);
            field = value;
        }
        
#endregion
        
#region GameObject

        /// <summary>
        /// 添加游戏对象的网络变量的设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <param name="objectId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GameObjectSyncVarSetter(GameObject value, ref GameObject field, ulong dirty, Action<GameObject, GameObject> OnChanged, ref uint objectId)
        {
            if (!GameObjectSyncVarEqual(value, objectId))
            {
                GameObject oldValue = field;
                SetGameObjectSyncVar(value, ref field, dirty, ref objectId);
                if (OnChanged != null)
                {
                    if (NetworkManager.mode == NetworkMode.Host && !GetSyncVarHook(dirty))
                    {
                        SetSyncVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetSyncVarHook(dirty, false);
                    }
                }
            }
        }

        /// <summary>
        /// 添加游戏对象的网络变量的访问器
        /// </summary>
        /// <param name="field"></param>
        /// <param name="OnChanged"></param>
        /// <param name="reader"></param>
        /// <param name="objectId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GameObjectSyncVarGetter(ref GameObject field, Action<GameObject, GameObject> OnChanged, NetworkReader reader, ref uint objectId)
        {
            uint oldId = objectId;
            GameObject oldObject = field;
            objectId = reader.ReadUInt();
            field = GetGameObjectSyncVar(objectId, ref field);
            if (OnChanged != null && !GeneralSyncVarEqual(oldId, ref objectId))
            {
                OnChanged(oldObject, field);
            }
        }
        
        /// <summary>
        /// 游戏对象的网络变量的比较器
        /// </summary>
        /// <param name="newObject"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        private static bool GameObjectSyncVarEqual(GameObject newObject, uint objectId)
        {
            uint newId = 0;
            if (newObject != null)
            {
                if (newObject.TryGetComponent(out NetworkObject @object))
                {
                    newId = @object.objectId;
                    if (newId == 0)
                    {
                        Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{newObject.name}");
                    }
                }
            }

            return newId == objectId;
        }
        
        /// <summary>
        /// 获取游戏对象的网络变量
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        private GameObject GetGameObjectSyncVar(uint objectId, ref GameObject field)
        {
            if (isServer || !isClient)
            {
                return field;
            }
            
            if (NetworkClient.spawns.TryGetValue(objectId,out var oldObject) && oldObject != null)
            {
                return field = oldObject.gameObject;
            }
            return null;
        }

        /// <summary>
        /// 设置游戏对象的网络变量
        /// </summary>
        /// <param name="newObject"></param>
        /// <param name="objectField"></param>
        /// <param name="dirty"></param>
        /// <param name="objectId"></param>
        private void SetGameObjectSyncVar(GameObject newObject, ref GameObject objectField, ulong dirty, ref uint objectId)
        {
            if (GetSyncVarHook(dirty)) return;
            uint newId = 0;
            if (newObject != null)
            {
                if (newObject.TryGetComponent(out NetworkObject identity))
                {
                    newId = identity.objectId;
                    if (newId == 0)
                    {
                        Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{newObject.name}");
                    }
                }
            }
            
            SetSyncVarDirty(dirty);
            objectField = newObject;
            objectId = newId;
        }

#endregion
      
#region NetworkObject

        /// <summary>
        /// 添加网络对象的网络变量的设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <param name="objectId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NetworkObjectSyncVarSetter(NetworkObject value, ref NetworkObject field, ulong dirty, Action<NetworkObject, NetworkObject> OnChanged, ref uint objectId)
        {
            if (!NetworkObjectSyncVarEqual(value, objectId))
            {
                NetworkObject oldValue = field;
                SetNetworkObjectSyncVar(value, ref field, dirty, ref objectId);
                if (OnChanged != null)
                {
                    if (NetworkManager.mode == NetworkMode.Host  && !GetSyncVarHook(dirty))
                    {
                        SetSyncVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetSyncVarHook(dirty, false);
                    }
                }
            }
        }

        /// <summary>
        /// 添加网络对象的网络变量的访问器
        /// </summary>
        /// <param name="field"></param>
        /// <param name="OnChanged"></param>
        /// <param name="reader"></param>
        /// <param name="objectId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NetworkObjectSyncVarGetter(ref NetworkObject field, Action<NetworkObject, NetworkObject> OnChanged, NetworkReader reader, ref uint objectId)
        {
            uint oldId = objectId;
            NetworkObject oldObject = field;
            objectId = reader.ReadUInt();
            field = GetNetworkObjectSyncVar(objectId, ref field);
            if (OnChanged != null && !GeneralSyncVarEqual(oldId, ref objectId))
            {
                OnChanged(oldObject, field);
            }
        }

        /// <summary>
        /// 网络对象的网络变量的比较器
        /// </summary>
        /// <param name="object"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        private static bool NetworkObjectSyncVarEqual(NetworkObject @object, uint objectId)
        {
            uint newId = 0;
            if (@object != null)
            {
                newId = @object.objectId;
                if (newId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            return newId == objectId;
        }

        /// <summary>
        /// 获取网络对象的网络变量
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="object"></param>
        /// <returns></returns>
        private NetworkObject GetNetworkObjectSyncVar(uint objectId, ref NetworkObject @object)
        {
            if (isServer || !isClient) return @object;
            NetworkClient.spawns.TryGetValue(objectId, out @object);
            return @object;
        }

        
        /// <summary>
        /// 设置网络对象的网络变量
        /// </summary>
        /// <param name="object"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="objectId"></param>
        private void SetNetworkObjectSyncVar(NetworkObject @object, ref NetworkObject field, ulong dirty, ref uint objectId)
        {
            if (GetSyncVarHook(dirty)) return;
            uint newId = 0;
            if (@object != null)
            {
                newId = @object.objectId;
                if (newId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            SetSyncVarDirty(dirty);
            objectId = newId;
            field = @object;
        }
        
#endregion
        
#region NetworkEntity
        
        /// <summary>
        /// 添加网络实体的网络变量的设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <param name="objectId"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NetworkEntitySyncVarSetter<T>(T value, ref T field, ulong dirty, Action<T, T> OnChanged, ref NetworkVariable objectId) where T : NetworkEntity
        {
            if (!NetworkEntitySyncVarEqual(value, objectId))
            {
                T oldValue = field;
                SetNetworkEntitySyncVar(value, ref field, dirty, ref objectId);
                if (OnChanged != null)
                {
                    if (NetworkManager.mode == NetworkMode.Host && !GetSyncVarHook(dirty))
                    {
                        SetSyncVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetSyncVarHook(dirty, false);
                    }
                }
            }
        }

        /// <summary>
        /// 添加网络实体的网络变量的访问器
        /// </summary>
        /// <param name="field"></param>
        /// <param name="OnChanged"></param>
        /// <param name="reader"></param>
        /// <param name="objectId"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NetworkEntitySyncVarGetter<T>(ref T field, Action<T, T> OnChanged, NetworkReader reader, ref NetworkVariable objectId) where T : NetworkEntity
        {
            NetworkVariable oldId = objectId;
            T oldObject = field;
            objectId = reader.ReadNetworkValue();
            field = GetNetworkEntitySyncVar(objectId, ref field);
            if (OnChanged != null && !GeneralSyncVarEqual(oldId, ref objectId))
            {
                OnChanged(oldObject, field);
            }
        }

        /// <summary>
        /// 网络实体的网络变量的比较器
        /// </summary>
        /// <param name="object"></param>
        /// <param name="objectId"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static bool NetworkEntitySyncVarEqual<T>(T @object, NetworkVariable objectId) where T : NetworkEntity
        {
            uint newId = 0;
            byte index = 0;
            if (@object != null)
            {
                newId = @object.objectId;
                index = @object.serialId;
                if (newId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }
            
            return objectId.Equals(newId, index);
        }
        
        /// <summary>
        /// 获取网络实体的网络变量
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T GetNetworkEntitySyncVar<T>(NetworkVariable value, ref T field) where T : NetworkEntity
        {
            if (isServer || !isClient)
            {
                return field;
            }
            
            if (!NetworkClient.spawns.TryGetValue(value.objectId,out var oldObject))
            {
                return null;
            }

            field = (T)oldObject.entities[value.serialId];
            return field;
        }

        /// <summary>
        /// 设置网络实体的网络变量
        /// </summary>
        /// <param name="object"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="netId"></param>
        /// <typeparam name="T"></typeparam>
        private void SetNetworkEntitySyncVar<T>(T @object, ref T field, ulong dirty, ref NetworkVariable netId) where T : NetworkEntity
        {
            if (GetSyncVarHook(dirty)) return;

            uint newId = 0;
            byte index = 0;
            if (@object != null)
            {
                newId = @object.objectId;
                index = @object.serialId;
                if (newId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            netId = new NetworkVariable(newId, index);
            SetSyncVarDirty(dirty);
            field = @object;
        }
        
#endregion
    }
}