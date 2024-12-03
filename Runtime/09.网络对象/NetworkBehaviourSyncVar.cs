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
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    /// <summary>
    /// 网络变量模块
    /// </summary>
    public abstract partial class NetworkBehaviour
    {
        /// <summary>
        /// 添加基础网络变量的设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarSetterGeneral<T>(T value, ref T field, ulong dirty, Action<T, T> OnChanged)
        {
            if (!SyncVarEqualGeneral(value, ref field))
            {
                T oldValue = field;
                SetSyncVarGeneral(value, ref field, dirty);
                if (OnChanged != null)
                {
                    if (NetworkManager.Mode == EntryMode.Host && !GetSyncVarHook(dirty))
                    {
                        SetSyncVarHook(dirty, true);
                        OnChanged(oldValue, value);
                        SetSyncVarHook(dirty, false);
                    }
                }
            }
        }

        /// <summary>
        /// 添加基础网络变量的访问器
        /// </summary>
        /// <param name="field"></param>
        /// <param name="OnChanged"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarGetterGeneral<T>(ref T field, Action<T, T> OnChanged, T value)
        {
            T oldValue = field;
            field = value;
            if (OnChanged != null && !SyncVarEqualGeneral(oldValue, ref field))
            {
                OnChanged(oldValue, field);
            }
        }

        /// <summary>
        /// 基础网络变量的值比较器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static bool SyncVarEqualGeneral<T>(T value, ref T field)
        {
            return EqualityComparer<T>.Default.Equals(value, field);
        }

        /// <summary>
        /// 基础网络变量的设置
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <typeparam name="T"></typeparam>
        // ReSharper disable once RedundantAssignment
        private void SetSyncVarGeneral<T>(T value, ref T field, ulong dirty)
        {
            SetSyncVarDirty(dirty);
            field = value;
        }

        /// <summary>
        /// 添加游戏对象的网络变量的设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <param name="objectId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarSetterGameObject(GameObject value, ref GameObject field, ulong dirty, Action<GameObject, GameObject> OnChanged, ref uint objectId)
        {
            if (!SyncVarEqualGameObject(value, objectId))
            {
                GameObject oldValue = field;
                SetSyncVarGameObject(value, ref field, dirty, ref objectId);
                if (OnChanged != null)
                {
                    if (NetworkManager.Mode == EntryMode.Host && !GetSyncVarHook(dirty))
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
        public void SyncVarGetterGameObject(ref GameObject field, Action<GameObject, GameObject> OnChanged, NetworkReader reader, ref uint objectId)
        {
            uint oldValue = objectId;
            GameObject oldObject = field;
            objectId = reader.ReadUInt();
            field = GetSyncVarGameObject(objectId, ref field);
            if (OnChanged != null && !SyncVarEqualGeneral(oldValue, ref objectId))
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
        private static bool SyncVarEqualGameObject(GameObject newObject, uint objectId)
        {
            uint newValue = 0;
            if (newObject != null)
            {
                if (newObject.TryGetComponent(out NetworkObject @object))
                {
                    newValue = @object.objectId;
                    if (newValue == 0)
                    {
                        Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{newObject.name}");
                    }
                }
            }

            return newValue == objectId;
        }

        /// <summary>
        /// 获取游戏对象的网络变量
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        private GameObject GetSyncVarGameObject(uint objectId, ref GameObject field)
        {
            if (isServer || !isClient)
            {
                return field;
            }

            if (NetworkManager.Client.spawns.TryGetValue(objectId, out var oldObject) && oldObject != null)
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
        private void SetSyncVarGameObject(GameObject newObject, ref GameObject objectField, ulong dirty, ref uint objectId)
        {
            if (GetSyncVarHook(dirty)) return;
            uint newValue = 0;
            if (newObject != null)
            {
                if (newObject.TryGetComponent(out NetworkObject entity))
                {
                    newValue = entity.objectId;
                    if (newValue == 0)
                    {
                        Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{newObject.name}");
                    }
                }
            }

            SetSyncVarDirty(dirty);
            objectField = newObject;
            objectId = newValue;
        }

        /// <summary>
        /// 添加网络对象的网络变量的设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <param name="objectId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarSetterNetworkObject(NetworkObject value, ref NetworkObject field, ulong dirty, Action<NetworkObject, NetworkObject> OnChanged, ref uint objectId)
        {
            if (!SyncVarEqualNetworkObject(value, objectId))
            {
                NetworkObject oldValue = field;
                SetSyncVarNetworkObject(value, ref field, dirty, ref objectId);
                if (OnChanged != null)
                {
                    if (NetworkManager.Mode == EntryMode.Host && !GetSyncVarHook(dirty))
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
        public void SyncVarGetterNetworkObject(ref NetworkObject field, Action<NetworkObject, NetworkObject> OnChanged, NetworkReader reader, ref uint objectId)
        {
            uint oldValue = objectId;
            NetworkObject oldObject = field;
            objectId = reader.ReadUInt();
            field = GetSyncVarNetworkObject(objectId, ref field);
            if (OnChanged != null && !SyncVarEqualGeneral(oldValue, ref objectId))
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
        private static bool SyncVarEqualNetworkObject(NetworkObject @object, uint objectId)
        {
            uint newValue = 0;
            if (@object != null)
            {
                newValue = @object.objectId;
                if (newValue == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            return newValue == objectId;
        }

        /// <summary>
        /// 获取网络对象的网络变量
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="object"></param>
        /// <returns></returns>
        private NetworkObject GetSyncVarNetworkObject(uint objectId, ref NetworkObject @object)
        {
            if (isServer || !isClient) return @object;
            NetworkManager.Client.spawns.TryGetValue(objectId, out @object);
            return @object;
        }


        /// <summary>
        /// 设置网络对象的网络变量
        /// </summary>
        /// <param name="object"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="objectId"></param>
        private void SetSyncVarNetworkObject(NetworkObject @object, ref NetworkObject field, ulong dirty, ref uint objectId)
        {
            if (GetSyncVarHook(dirty)) return;
            uint newValue = 0;
            if (@object != null)
            {
                newValue = @object.objectId;
                if (newValue == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            SetSyncVarDirty(dirty);
            objectId = newValue;
            field = @object;
        }

        /// <summary>
        /// 添加网络实体的网络变量的设置器
        /// </summary>
        /// <param name="value"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="OnChanged"></param>
        /// <param name="variable"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarSetterNetworkBehaviour<T>(T value, ref T field, ulong dirty, Action<T, T> OnChanged, ref NetworkVariable variable) where T : NetworkBehaviour
        {
            if (!SyncVarEqualNetworkBehaviour(value, variable))
            {
                T oldValue = field;
                SetSyncVarNetworkBehaviour(value, ref field, dirty, ref variable);
                if (OnChanged != null)
                {
                    if (NetworkManager.Mode == EntryMode.Host && !GetSyncVarHook(dirty))
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
        /// <param name="variable"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncVarGetterNetworkBehaviour<T>(ref T field, Action<T, T> OnChanged, NetworkReader reader, ref NetworkVariable variable) where T : NetworkBehaviour
        {
            var oldValue = variable;
            T oldObject = field;
            variable = reader.ReadNetworkVariable();
            field = GetSyncVarNetworkBehaviour(variable, ref field);
            if (OnChanged != null && !SyncVarEqualGeneral(oldValue, ref variable))
            {
                OnChanged(oldObject, field);
            }
        }

        /// <summary>
        /// 网络实体的网络变量的比较器
        /// </summary>
        /// <param name="object"></param>
        /// <param name="variable"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static bool SyncVarEqualNetworkBehaviour<T>(T @object, NetworkVariable variable) where T : NetworkBehaviour
        {
            uint newValue = 0;
            byte index = 0;
            if (@object != null)
            {
                newValue = @object.objectId;
                index = @object.componentId;
                if (newValue == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            return variable.Equals(newValue, index);
        }

        /// <summary>
        /// 获取网络实体的网络变量
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="field"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSyncVarNetworkBehaviour<T>(NetworkVariable variable, ref T field) where T : NetworkBehaviour
        {
            if (isServer || !isClient)
            {
                return field;
            }

            if (!NetworkManager.Client.spawns.TryGetValue(variable.objectId, out var oldObject))
            {
                return null;
            }

            field = (T)oldObject.entities[variable.componentId];
            return field;
        }

        /// <summary>
        /// 设置网络实体的网络变量
        /// </summary>
        /// <param name="object"></param>
        /// <param name="field"></param>
        /// <param name="dirty"></param>
        /// <param name="variable"></param>
        /// <typeparam name="T"></typeparam>
        private void SetSyncVarNetworkBehaviour<T>(T @object, ref T field, ulong dirty, ref NetworkVariable variable) where T : NetworkBehaviour
        {
            if (GetSyncVarHook(dirty)) return;
            uint newValue = 0;
            byte index = 0;
            if (@object != null)
            {
                newValue = @object.objectId;
                index = @object.componentId;
                if (newValue == 0)
                {
                    Debug.LogWarning($"设置网络变量的对象未生成。对象名称：{@object.name}");
                }
            }

            variable = new NetworkVariable(newValue, index);
            SetSyncVarDirty(dirty);
            field = @object;
        }
    }
}