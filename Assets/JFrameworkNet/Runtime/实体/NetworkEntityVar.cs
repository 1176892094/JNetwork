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
        public void AddServerVarSetter<T>(T value, ref T field, ulong dirty, Action<T, T> OnChanged)
        {
            if (!ServerVarEqual(value, ref field))
            {
                T oldValue = field;
                SetServerVar(value, ref field, dirty);
                if (OnChanged != null)
                {
                    if (NetworkManager.mode == NetworkMode.Host && !GetServerVarHook(dirty))
                    {
                        SetServerVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetServerVarHook(dirty, false);
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
        public void AddServerVarGetter<T>(ref T field, Action<T, T> OnChanged, T value)
        {
            T oldValue = field;
            field = value;
            if (OnChanged != null && !ServerVarEqual(oldValue, ref field))
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
        private static bool ServerVarEqual<T>(T value, ref T field)
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
        private void SetServerVar<T>(T value, ref T field, ulong dirty)
        {
            SetServerVarDirty(dirty);
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
        public void AddServerVarSetter(GameObject value, ref GameObject field, ulong dirty, Action<GameObject, GameObject> OnChanged, ref uint objectId)
        {
            if (!ServerVarEqual(value, objectId))
            {
                GameObject oldValue = field;
                SetServerVar(value, ref field, dirty, ref objectId);
                if (OnChanged != null)
                {
                    if (NetworkManager.mode == NetworkMode.Host && !GetServerVarHook(dirty))
                    {
                        SetServerVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetServerVarHook(dirty, false);
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
        public void AddServerVarGetter(ref GameObject field, Action<GameObject, GameObject> OnChanged, NetworkReader reader, ref uint objectId)
        {
            uint oldId = objectId;
            GameObject oldObject = field;
            objectId = reader.ReadUInt();
            field = GetServerVar(objectId, ref field);
            if (OnChanged != null && !ServerVarEqual(oldId, ref objectId))
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
        private static bool ServerVarEqual(GameObject newObject, uint objectId)
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
        private GameObject GetServerVar(uint objectId, ref GameObject field)
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
        private void SetServerVar(GameObject newObject, ref GameObject objectField, ulong dirty, ref uint objectId)
        {
            if (GetServerVarHook(dirty)) return;
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
            
            SetServerVarDirty(dirty);
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
        public void AddServerVarSetter(NetworkObject value, ref NetworkObject field, ulong dirty, Action<NetworkObject, NetworkObject> OnChanged, ref uint objectId)
        {
            if (!ServerVarEqual(value, objectId))
            {
                NetworkObject oldValue = field;
                SetServerVar(value, ref field, dirty, ref objectId);
                if (OnChanged != null)
                {
                    if (NetworkManager.mode == NetworkMode.Host  && !GetServerVarHook(dirty))
                    {
                        SetServerVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetServerVarHook(dirty, false);
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
        public void AddServerVarGetter(ref NetworkObject field, Action<NetworkObject, NetworkObject> OnChanged, NetworkReader reader, ref uint objectId)
        {
            uint oldId = objectId;
            NetworkObject oldObject = field;
            objectId = reader.ReadUInt();
            field = GetServerVar(objectId, ref field);
            if (OnChanged != null && !ServerVarEqual(oldId, ref objectId))
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
        private static bool ServerVarEqual(NetworkObject @object, uint objectId)
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
        private NetworkObject GetServerVar(uint objectId, ref NetworkObject @object)
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
        private void SetServerVar(NetworkObject @object, ref NetworkObject field, ulong dirty, ref uint objectId)
        {
            if (GetServerVarHook(dirty)) return;
            uint newId = 0;
            if (@object != null)
            {
                newId = @object.objectId;
                if (newId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            SetServerVarDirty(dirty);
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
        public void AddServerVarSetter<T>(T value, ref T field, ulong dirty, Action<T, T> OnChanged, ref NetworkVariable objectId) where T : NetworkEntity
        {
            if (!ServerVarEqual(value, objectId))
            {
                T oldValue = field;
                SetServerVar(value, ref field, dirty, ref objectId);
                if (OnChanged != null)
                {
                    if (NetworkManager.mode == NetworkMode.Host && !GetServerVarHook(dirty))
                    {
                        SetServerVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetServerVarHook(dirty, false);
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
        public void AddServerVarGetter<T>(ref T field, Action<T, T> OnChanged, NetworkReader reader, ref NetworkVariable objectId) where T : NetworkEntity
        {
            NetworkVariable oldId = objectId;
            T oldObject = field;
            objectId = reader.ReadNetworkValue();
            field = GetServerVar(objectId, ref field);
            if (OnChanged != null && !ServerVarEqual(oldId, ref objectId))
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
        private static bool ServerVarEqual<T>(T @object, NetworkVariable objectId) where T : NetworkEntity
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
        private T GetServerVar<T>(NetworkVariable value, ref T field) where T : NetworkEntity
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
        private void SetServerVar<T>(T @object, ref T field, ulong dirty, ref NetworkVariable netId) where T : NetworkEntity
        {
            if (GetServerVarHook(dirty)) return;

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
            SetServerVarDirty(dirty);
            field = @object;
        }
        
#endregion
    }
}