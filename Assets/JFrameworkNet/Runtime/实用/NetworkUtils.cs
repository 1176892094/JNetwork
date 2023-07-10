using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkUtils
    {
        /// <summary>
        /// 是场景物体
        /// </summary>
        public static bool IsSceneObject(NetworkObject entity)
        {
            var gameObject = entity.gameObject;
            if (entity.sceneId == 0) return false;
            return gameObject.hideFlags is not (HideFlags.HideAndDontSave or HideFlags.NotEditable);
        }

        /// <summary>
        /// 拥有有效的父物体
        /// </summary>
        public static bool IsValidParent(NetworkObject entity)
        {
            var parent = entity.transform.parent;
            return parent == null || parent.gameObject.activeInHierarchy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkObject GetNetworkIdentity(uint netId)
        {
            // if (NetworkServer.isActive)
            // {
            //     NetworkServer.TryGetNetId(netId, out var identity);
            //     return identity;
            // }
            //
            // if (NetworkClient.isActive)
            // {
            //     NetworkClient.TryGetNetId(netId, out var identity);
            //     return identity;
            // }

            return null;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Elapsed(double current, double interval, ref double lastTime)
        {
            if (current < lastTime + interval) return false;
            var clientTime = (long)(current / interval);
            lastTime = clientTime * interval;
            return true;
        }
    }
}