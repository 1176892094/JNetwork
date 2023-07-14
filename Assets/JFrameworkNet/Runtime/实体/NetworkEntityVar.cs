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
                        OnChanged.Invoke(oldValue, value);
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
        /// <param name="netId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddServerVarSetter(GameObject value, ref GameObject field, ulong dirty, Action<GameObject, GameObject> OnChanged, ref uint netId)
        {
            if (!ServerVarEqual(value, netId))
            {
                GameObject oldValue = field;
                SetServerVar(value, ref field, dirty, ref netId);
                if (OnChanged != null)
                {
                    if (NetworkManager.mode == NetworkMode.Host && !GetServerVarHook(dirty))
                    {
                        SetServerVarHook(dirty, true);
                        OnChanged.Invoke(oldValue, value);
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
        /// <param name="netId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddServerVarGetter(ref GameObject field, Action<GameObject, GameObject> OnChanged, NetworkReader reader, ref uint netId)
        {
            uint oldNetId = netId;
            GameObject oldObject = field;
            netId = reader.ReadUInt();
            field = GetServerVar(netId, ref field);
            if (OnChanged != null && !ServerVarEqual(oldNetId, ref netId))
            {
                OnChanged(oldObject, field);
            }
        }
        
        /// <summary>
        /// 游戏对象的网络变量的比较器
        /// </summary>
        /// <param name="newObject"></param>
        /// <param name="netId"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        private static bool ServerVarEqual(GameObject newObject, uint netId)
        {
            uint newNetId = 0;
            if (newObject != null)
            {
                if (newObject.TryGetComponent(out NetworkObject @object))
                {
                    newNetId = @object.netId;
                    if (newNetId == 0)
                    {
                        Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{newObject.name}");
                    }
                }
            }

            return newNetId == netId;
        }
        
        /// <summary>
        /// 获取游戏对象的网络变量
        /// </summary>
        /// <param name="netId"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        private GameObject GetServerVar(uint netId, ref GameObject field)
        {
            if (isServer || !isClient)
            {
                return field;
            }
            
            if (NetworkClient.spawns.TryGetValue(netId,out var oldObject) && oldObject != null)
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
        /// <param name="netId"></param>
        private void SetServerVar(GameObject newObject, ref GameObject objectField, ulong dirty, ref uint netId)
        {
            if (GetServerVarHook(dirty)) return;
            uint newNetId = 0;
            if (newObject != null)
            {
                if (newObject.TryGetComponent(out NetworkObject identity))
                {
                    newNetId = identity.netId;
                    if (newNetId == 0)
                    {
                        Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{newObject.name}");
                    }
                }
            }
            
            SetServerVarDirty(dirty);
            objectField = newObject;
            netId = newNetId;
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
        /// <param name="netId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddServerVarSetter(NetworkObject value, ref NetworkObject field, ulong dirty, Action<NetworkObject, NetworkObject> OnChanged, ref uint netId)
        {
            if (!ServerVarEqual(value, netId))
            {
                NetworkObject oldValue = field;
                SetServerVar(value, ref field, dirty, ref netId);
                if (OnChanged != null)
                {
                    if (NetworkManager.mode == NetworkMode.Host  && !GetServerVarHook(dirty))
                    {
                        SetServerVarHook(dirty, true);
                        OnChanged.Invoke(oldValue, value);
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
        /// <param name="netId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddServerVarGetter(ref NetworkObject field, Action<NetworkObject, NetworkObject> OnChanged, NetworkReader reader, ref uint netId)
        {
            uint oldNetId = netId;
            NetworkObject oldObject = field;
            netId = reader.ReadUInt();
            field = GetServerVar(netId, ref field);
            if (OnChanged != null && !ServerVarEqual(oldNetId, ref netId))
            {
                OnChanged(oldObject, field);
            }
        }

        /// <summary>
        /// 网络对象的网络变量的比较器
        /// </summary>
        /// <param name="object"></param>
        /// <param name="netId"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        private static bool ServerVarEqual(NetworkObject @object, uint netId)
        {
            uint newNetId = 0;
            if (@object != null)
            {
                newNetId = @object.netId;
                if (newNetId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }
            return newNetId == netId;
        }
        
        /// <summary>
        /// 获取网络对象的网络变量
        /// </summary>
        /// <param name="netId"></param>
        /// <param name="object"></param>
        /// <returns></returns>
        private NetworkObject GetServerVar(uint netId, ref NetworkObject @object)
        {
            if (isServer || !isClient) return @object;
            NetworkClient.spawns.TryGetValue(netId, out @object);
            return @object;
        }

        
        /// <summary>
        /// 设置网络对象的网络变量
        /// </summary>
        /// <param name="object"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="netId"></param>
        private void SetServerVar(NetworkObject @object, ref NetworkObject field, ulong dirty, ref uint netId)
        {
            if (GetServerVarHook(dirty)) return;
            uint newNetId = 0;
            if (@object != null)
            {
                newNetId = @object.netId;
                if (newNetId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            SetServerVarDirty(dirty);
            netId = newNetId;
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
        /// <param name="netId"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddServerVarSetter<T>(T value, ref T field, ulong dirty, Action<T, T> OnChanged, ref NetworkValue netId) where T : NetworkEntity
        {
            if (!ServerVarEqual(value, netId))
            {
                T oldValue = field;
                SetServerVar(value, ref field, dirty, ref netId);
                if (OnChanged != null)
                {
                    if (NetworkManager.mode == NetworkMode.Host && !GetServerVarHook(dirty))
                    {
                        SetServerVarHook(dirty, true);
                        OnChanged.Invoke(oldValue, value);
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
        /// <param name="netIdField"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddServerVarGetter<T>(ref T field, Action<T, T> OnChanged, NetworkReader reader, ref NetworkValue netIdField) where T : NetworkEntity
        {
            NetworkValue previousNetId = netIdField;
            T previousBehaviour = field;
            netIdField = reader.ReadNetworkValue();
            field = GetServerVar(netIdField, ref field);
            if (OnChanged != null && !ServerVarEqual(previousNetId, ref netIdField))
            {
                OnChanged(previousBehaviour, field);
            }
        }

        /// <summary>
        /// 网络实体的网络变量的比较器
        /// </summary>
        /// <param name="object"></param>
        /// <param name="netId"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static bool ServerVarEqual<T>(T @object, NetworkValue netId) where T : NetworkEntity
        {
            uint newNetId = 0;
            byte componentIndex = 0;
            if (@object != null)
            {
                newNetId = @object.netId;
                componentIndex = @object.component;
                if (newNetId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }
            
            return netId.Equals(newNetId, componentIndex);
        }
        
        /// <summary>
        /// 获取网络实体的网络变量
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T GetServerVar<T>(NetworkValue value, ref T field) where T : NetworkEntity
        {
            if (isServer || !isClient)
            {
                return field;
            }
            
            if (!NetworkClient.spawns.TryGetValue(value.netId,out var oldObject))
            {
                return null;
            }

            field = (T)oldObject.objects[value.index];
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
        private void SetServerVar<T>(T @object, ref T field, ulong dirty, ref NetworkValue netId) where T : NetworkEntity
        {
            if (GetServerVarHook(dirty)) return;

            uint newNetId = 0;
            byte componentIndex = 0;
            if (@object != null)
            {
                newNetId = @object.netId;
                componentIndex = @object.component;
                if (newNetId == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            netId = new NetworkValue(newNetId, componentIndex);
            SetServerVarDirty(dirty);
            field = @object;
        }
        
#endregion
    }
}