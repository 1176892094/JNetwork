using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public abstract partial class NetworkEntity
    {
        /// <summary>
        /// 服务器变量的改变选项
        /// </summary>
        protected ulong serverVarDirty;
        
        /// <summary>
        /// 服务器对象的改变选项
        /// </summary>
        internal ulong serverObjectDirty;
        
        /// <summary>
        /// 服务器变量的钩子
        /// </summary>
        private ulong serverVarHook;

        /// <summary>
        /// 设置服务器变量改变
        /// </summary>
        /// <param name="dirty"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetServerVarDirty(ulong dirty) => serverVarDirty |= dirty;
        
        /// <summary>
        /// 获取服务器变量的钩子
        /// </summary>
        /// <param name="dirty"></param>
        /// <returns></returns>
        private bool GetServerVarHook(ulong dirty) => (serverVarHook & dirty) != 0UL;

        /// <summary>
        /// 设置服务器变量的钩子
        /// </summary>
        /// <param name="dirty"></param>
        /// <param name="value"></param>
        private void SetServerVarHook(ulong dirty, bool value)
        {
            serverVarHook = value ? serverVarHook | dirty : serverVarHook & ~dirty;
        }
        
#region ServerVar

        /// <summary>
        /// 添加标准的服务器变量设置
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
        /// 服务器变量的值比较
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
        /// 设置服务器变量的值
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
        /// 添加游戏对象的网络变量
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
        /// 比较游戏对象的网络变量是否改变
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
        /// 设置游戏对象的网络变量值
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
        /// 设置网络对象的网络变量
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
        /// 网络对象的网络变量
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
        /// 设置网络变量的网络对象
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
        /// 添加网络实体的网络变量
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
        /// 判断网络变量是否相等
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
        /// 设置网络变量的网络实体值
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