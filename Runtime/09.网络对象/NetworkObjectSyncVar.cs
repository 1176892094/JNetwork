// *********************************************************************************
// # Project: Forest
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-12-03  14:12
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;

namespace JFramework.Net
{
    public sealed partial class NetworkObject
    {
        /// <summary>
        /// 选择性地将需要更新的组件数据序列化并发送给所有者和观察者，以减少不必要的数据传输
        /// </summary>
        /// <param name="status"></param>
        /// <param name="owner"></param>
        /// <param name="observer"></param>
        internal void ServerSerialize(bool status, NetworkWriter owner, NetworkWriter observer)
        {
            var components = entities;
            var (ownerMask, observerMask) = ServerDirtyMasks(status);

            if (ownerMask != 0)
            {
                NetworkCompress.CompressVarUInt(owner, ownerMask);
            }

            if (observerMask != 0)
            {
                NetworkCompress.CompressVarUInt(observer, observerMask);
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
                        component.Serialize(writer, status);
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
        /// 客户端序列化 SyncVar
        /// </summary>
        /// <param name="writer"></param>
        internal void ClientSerialize(NetworkWriter writer)
        {
            var components = entities;
            var dirtyMask = ClientDirtyMask();
            if (dirtyMask != 0)
            {
                NetworkCompress.CompressVarUInt(writer, dirtyMask);
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
        /// 服务器反序列化 SyncVar
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal bool ServerDeserialize(NetworkReader reader)
        {
            var components = entities;
            var mask = NetworkCompress.DecompressVarUInt(reader);

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
        /// <param name="status"></param>
        internal void ClientDeserialize(NetworkReader reader, bool status)
        {
            var components = entities;
            var mask = NetworkCompress.DecompressVarUInt(reader);

            for (int i = 0; i < components.Length; ++i)
            {
                if (IsDirty(mask, i))
                {
                    var component = components[i];
                    component.Deserialize(reader, status);
                }
            }
        }

        /// <summary>
        /// 用于指示哪些组件需要被序列化并发送给所有者和观察者
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        private (ulong, ulong) ServerDirtyMasks(bool status)
        {
            ulong ownerMask = 0;
            ulong observerMask = 0;

            var components = entities;
            for (int i = 0; i < components.Length; ++i)
            {
                var component = components[i];
                var dirty = component.IsDirty();
                ulong mask = 1U << i;
                if (status || (component.syncDirection == SyncMode.Server && dirty))
                {
                    ownerMask |= mask;
                }

                if (status || dirty)
                {
                    observerMask |= mask;
                }
            }

            return (ownerMask, observerMask);
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
                if ((entityMode & EntityMode.Owner) == EntityMode.Owner && component.syncDirection == SyncMode.Client)
                {
                    if (component.IsDirty()) mask |= 1U << i;
                }
            }

            return mask;
        }
    }
}